using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FoodSplitApp.Errors;
using FoodSplitApp.Model;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.AspNetCore;
using SlackNet.Blocks;
using SlackNet.Interaction;
using SlackNet.WebApi;

namespace FoodSplitApp.Services.Slack
{
    public class SlackCommandHandler : ISlashCommandHandler
    {
        private readonly IFoodService foodService;
        protected readonly ISlackApiClient slack;
        private readonly ExecutionContext context;

        public SlackCommandHandler(
            ISlackApiClient slack,
            IFoodService foodService,
            ExecutionContext context)
        {
            this.slack = slack;
            this.foodService = foodService;
            this.context = context;
        }

        public async Task<SlashCommandResponse> Handle(SlashCommand command)
        {
            SetContext(command.TeamId, command.UserName, command.UserId);

            try
            {
                var args = command.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Alternative commands were added quite late so maybe now this should be refactored into collection if-else check
                switch (args[0])
                {
                    case CommandTexts.OpenNewOrder:
                    case CommandTexts.OpenNewOrderAlt1:
                    case CommandTexts.OpenNewOrderAlt2:
                    case CommandTexts.OpenNewOrderAlt3:
                        await HandleOpenOrderAction(command);
                        break;

                    case CommandTexts.Cancel:
                        await HandleCancelLastOrderAction(command);
                        break;

                    case CommandTexts.EatAs:
                        await HandleEatAsOtherAction(command);
                        break;

                    case CommandTexts.Eat:
                    case CommandTexts.EatAlt1:
                        await HandleEatAsCallerAction(command);
                        break;

                    case CommandTexts.Shared:
                    case CommandTexts.SharedAlt1:
                        await HandleSharedCostAction(command);
                        break;

                    case CommandTexts.Reopen:
                        await HandleReopenOrderAction(command);
                        break;

                    case CommandTexts.Finish:
                        await HandleFinalizeOrderAction(command);
                        break;

                    case CommandTexts.Owe:
                        await HandleOweAction(command);
                        break;

                    case CommandTexts.Forgive:
                        await HandleForgiveAction(command);
                        break;

                    case CommandTexts.Balance:
                        await HandleShowBalanceAction(command);
                        break;
                }
            }
            catch (Exception x)
            {
                await SendError(command.ResponseUrl, x);
            }

            return new SlashCommandResponse();
        }

        /// <summary>
        /// Open a new order and publish it to slack.
        /// </summary>
        private async Task HandleOpenOrderAction(SlashCommand command)
        {
            var order = await foodService.OpenNewOrder(context.Caller);
            await SendCommandResponse(command, SlackFormatter.BuildOrderMessage(order));
        }

        /// <summary>
        /// Cancel whatever the order is currently in progress and notify slack.
        /// </summary>
        private async Task HandleCancelLastOrderAction(SlashCommand command)
        {
            await foodService.CancelOpenOrder();
            await SendPublicResponse(command, $"Order canceled by {command.UserName}.");
        }

        /// <summary>
        /// Finish the order, update balance and publish the updated balance to slack along with closed order message.
        /// </summary>

        private async Task HandleFinalizeOrderAction(SlashCommand command)
        {
            var (finishedOrder, updatedBalance) = await foodService.FinishOrder();
            var responseBlocks = new List<Block>();
            responseBlocks.AddRange(SlackFormatter.BuildOrderMessage(finishedOrder));
            responseBlocks.AddRange(SlackFormatter.BuildBalanceMessage(updatedBalance));

            await SendCommandResponse(command, responseBlocks);
        }

        /// <summary>
        /// Reopen last finished order, revert balance and notify slack.
        /// </summary>
        private async Task HandleReopenOrderAction(SlashCommand command)
        {
            var (reopenedOrder, updatedBalance) = await foodService.ReopenOrder();
            await SendPublicResponse(command, "Order reopened, reverting last balance changes.");

            var responseBlocks = new List<Block>();
            responseBlocks.AddRange(SlackFormatter.BuildBalanceMessage(updatedBalance));
            responseBlocks.AddRange(SlackFormatter.BuildOrderMessage(reopenedOrder));

            await SendCommandResponse(command, responseBlocks);
        }

        /// <summary>
        /// Add caller as eater to currently open order and publish updated order to slack.
        /// Update current entry, if already added.
        /// </summary>
        private async Task HandleEatAsCallerAction(SlashCommand command)
        {
            try
            {
                // Example: ['eat', '12.50', 'pizza']
                var args = command.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

                if (args.Count < 2)
                {
                    throw new FormatException();
                }

                // Cost
                args[1] = args[1].Replace(',', '.');
                var cost = decimal.Parse(args[1], CultureInfo.InvariantCulture);

                // Optional item description
                string item = null!;
                if (args.Count >= 3)
                {
                    // Join all remaining items because spaces may have been used in item description
                    item = string.Join(' ', args.Skip(2).ToArray());
                }

                var updatedOrder = await foodService.AddEater(eater: new FoodUser(command.UserId, command.UserName), cost, item);
                await SendCommandResponse(command, SlackFormatter.BuildOrderMessage(updatedOrder));
            }
            catch (FormatException)
            {
                await SendError(command.ResponseUrl,
                    new BadRequestException($"Incorrect format, expected: /{CommandTexts.Eat} <cost> [description]"));
            }
            catch (Exception x)
            {
                await SendError(command.ResponseUrl, x);
            }
        }

        /// <summary>
        /// Add someone else as eater to currently open order and publish updated order to slack.
        /// Update current entry, if already added.
        /// Can be used to host external guests.
        /// </summary>
        private async Task HandleEatAsOtherAction(SlashCommand command)
        {
            try
            {
                // Example: ['eat', 'batman', '12.50', 'pizza']
                var args = command.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

                if (args.Count < 3)
                {
                    throw new FormatException();
                }

                // Eater
                var eaterStr = args[1].Trim();
                var eater = FoodUser.CreateFromString(eaterStr);

                // Cost
                args[2] = args[2].Replace(',', '.');
                var cost = decimal.Parse(args[2], CultureInfo.InvariantCulture);

                // Optional item description
                string item = null!;
                if (args.Count >= 4)
                {
                    item = item = string.Join(' ', args.Skip(3).ToArray());
                }

                var updatedOrder = await foodService.AddEater(eater: eater, cost, item);
                await SendCommandResponse(command, SlackFormatter.BuildOrderMessage(updatedOrder));
            }
            catch (FormatException)
            {
                await SendError(command.ResponseUrl,
                    new BadRequestException($"Incorrect format, expected: `/food {CommandTexts.EatAs} <who> <cost> [description]`"));
            }
            catch (Exception x)
            {
                await SendError(command.ResponseUrl, x);
            }
        }

        /// <summary>
        /// Set cost (e.g. delivery) that should be shared equally by all eaters.
        /// Update current order and publish it to slack.
        /// </summary>
        private async Task HandleSharedCostAction(SlashCommand command)
        {
            try
            {
                // Example: ['shared', '4.99']
                var args = command.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length != 2)
                {
                    throw new FormatException();
                }

                args[1] = args[1].Replace(',', '.');
                var cost = decimal.Parse(args[1]);

                var updatedOrder = await foodService.SetSharedCost(cost);
                await SendCommandResponse(command, SlackFormatter.BuildOrderMessage(updatedOrder));
            }
            catch (FormatException)
            {
                await SendError(command.ResponseUrl,
                    new BadRequestException($"Incorrect format, expected: `/food {CommandTexts.Shared} <cost>`"));
            }
        }

        /// <summary>
        /// Add some debt by caller to mentioned user. Order-less way to alter balance.
        /// </summary>
        private async Task HandleOweAction(SlashCommand command)
        {
            try
            {
                // Example: ['owe', '@user' '4.99']
                var args = command.Text.Split(' ');
                if (args.Length != 3)
                {
                    throw new FormatException();
                }

                var creditor = FoodUser.CreateFromString(args[1].Trim());
                var debtor = new FoodUser(command.UserId, command.UserName);

                if (debtor.UniqueId == creditor.UniqueId)
                {
                    await SendError(command.ResponseUrl, new BadRequestException("You can't do it to yourself."));
                    return;
                }

                var creditValue = decimal.Parse(args[2].Replace(',', '.'));
                await foodService.OweCredit(debtor, creditor, creditValue);
            }
            catch (FormatException)
            {
                await SendError(command.ResponseUrl,
                    new BadRequestException($"Incorrect format, expected: `/food {CommandTexts.Owe} @<user> <cost>`"));
            }
        }

        /// <summary>
        /// Forgive mentioned user's debt fully. Order-less way to alter balance.
        /// </summary>
        private async Task HandleForgiveAction(SlashCommand command)
        {
            try
            {
                // Example: ['forgive', '@user']
                var args = command.Text.Split(' ');
                if (args.Length != 2)
                {
                    throw new FormatException();
                }

                var debtor = FoodUser.CreateFromString(args[1].Trim());
                var creditor = new FoodUser(command.UserId, command.UserName);

                if (debtor.UniqueId == creditor.UniqueId)
                {
                    await SendError(command.ResponseUrl, new BadRequestException("You can't do it to yourself."));
                    return;
                }

                await foodService.ResetCredit(debtor, creditor);
            }
            catch (FormatException)
            {
                await SendError(command.ResponseUrl,
                    new BadRequestException($"Incorrect format, expected: `/food {CommandTexts.Forgive} @<user>`"));
            }
        }

        /// <summary>
        /// Print current balance to slack publicly.
        /// </summary>

        private async Task HandleShowBalanceAction(SlashCommand command)
        {
            var balanceBook = await foodService.GetBalanceBook();
            if (balanceBook == null)
            {
                throw new BadRequestException("No balance is tracked.");
            }

            await SendCommandResponse(command, SlackFormatter.BuildBalanceMessage(balanceBook));
        }

        private async Task SendCommandResponse(SlashCommand command, IList<Block> msgBlocks)
        {
            var message = new SlashCommandResponse()
            {
                Message = new Message
                {
                    Blocks = msgBlocks,
                    Channel = command.ChannelId,
                    AsUser = false,
                },
                ResponseType = ResponseType.InChannel
            };

            // Add information about command caller
            var callerInfo = new PlainText($"Command from: {command.UserName}.");
            if (message.Message.Blocks.Last() is ContextBlock ctx)
            {
                ctx.Elements.Add(callerInfo);
            }
            else
            {
                message.Message.Blocks.Add(new ContextBlock()
                {
                    Elements = new List<IContextElement>()
                    {
                        callerInfo
                    }
                });
            }

            await slack.Respond(command, message);
        }

        /// <summary>
        /// Send a private message to given webhook to user that executed the command.
        /// </summary>
        private async Task SendPrivateResponse(string slackResponseUrl, string markdown)
        {
            await slack.Respond(slackResponseUrl, new Message
            {
                Blocks = new List<Block>
                {
                    new SectionBlock
                    {
                        Text = new Markdown(markdown)
                    }
                }
            }, CancellationToken.None);
        }

        /// <summary>
        /// Send channel message in response to given command.
        /// </summary>
        private async Task SendPublicResponse(SlashCommand command, string markdown)
        {
            var message = new SlashCommandResponse()
            {
                Message = new Message
                {
                    Blocks = new List<Block>()
                    {
                        new SectionBlock()
                        {
                            Text = new Markdown(markdown)
                        }
                    },
                    Channel = command.ChannelId,
                    AsUser = false,
                },
                ResponseType = ResponseType.InChannel
            };

            await slack.Respond(command, message);
        }

        /// <summary>
        /// Send an exception to given webhook as private message to user that executed faulted command.
        /// </summary>
        private async Task SendError(string slackResponseUrl, Exception exception)
        {
            if (exception is BadRequestException ex)
            {
                await SendPrivateResponse(slackResponseUrl, ex.Message);
            }
            else
            {
                await SendPrivateResponse(slackResponseUrl,
                    $"Something went wrong. Maybe this stack trace will help you: ```{exception}```");
            }
        }

        private void SetContext(string teamId, string userName, string userId)
        {
            context.TeamId = teamId;
            context.Caller = new SlackUser(userName, userId);
        }
    }
}