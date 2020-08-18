using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FoodSplitApp.Model;
using FoodSplitApp.Model.Balance;
using FoodSplitApp.Model.Orders;
using SlackNet;
using SlackNet.Blocks;

namespace FoodSplitApp.Services.Slack
{
    public class SlackFormatter
    {
        public static IList<Block> BuildBalanceMessage(BalanceBook balanceBook)
        {
            var blocks = new List<Block>();

            var balanceBlock = new SectionBlock()
            {
                BlockId = "balance_summary",
                Text = new Markdown("")
            };
            foreach (var pair in balanceBook.Balances.Values
                .Where(x => x.Balance != 0)
                .OrderBy(x => x.Key))
            {
                var balanceStr = Math.Abs(pair.Balance).ToString("0.00", CultureInfo.InvariantCulture);
                balanceStr = pair.Balance > 0
                    ? $" :arrow_left: {balanceStr} :arrow_left: "
                    : $" :arrow_right: {balanceStr} :arrow_right: ";

                balanceBlock.Text.Text += $"> {pair.PartyA.ToSlackMention()}{balanceStr}{pair.PartyB.ToSlackMention()}\n";
            }

            var (biggestDebtor, biggestDebt) = balanceBook.FindBiggestDebtor();
            if (biggestDebtor != null)
            {
                balanceBlock.Text.Text += $"{biggestDebtor.ToSlackMention()} should host the next order (total: *{biggestDebt:F2}*)";
            }

            if (string.IsNullOrWhiteSpace(balanceBlock.Text.Text))
            {
                balanceBlock.Text = new PlainText("Everyone is even.");
            }

            blocks.Add(balanceBlock);

            return blocks;
        }

        public static IList<Block> BuildOrderMessage(Order order)
        {
            var blocks = new List<Block>();
            /* Summary */
            var costSummary = new SectionBlock()
            {
                BlockId = "order_costs",
            };

            var payload = order.IsOpen
                ? new Markdown($":hourglass_flowing_sand: Order in progress by {order.Owner.FriendlyName}:\n")
                : new Markdown($":checkered_flag: Order complete by {order.Owner.FriendlyName}:\n");

            if (order.Costs != null)
            {
                foreach (var cost in order.Costs.Values.Where(x => x.Value > 0))
                {
                    payload.Text += $"{cost.MarkdownDescription}\n";
                }
            }

            if (order.SharedCost > 0)
            {
                payload.Text += $"> • shared: *{order.SharedCost}*\n";
            }

            payload.Text += $"> ---\n> Total: *{order.GetTotalCost()}*";
            costSummary.Text = payload;

            blocks.Add(costSummary);
            blocks.Add(new DividerBlock());

            var context = new ContextBlock
            {
                BlockId = "order_context",
                Elements = new List<IContextElement>()
            };

            if (order.IsOpen)
            {
                context.Elements.Add(new Markdown($"Available `/food ` commands: " +
                                                  $"`{CommandTexts.Eat} <value> [food]`, " +
                                                  $"`{CommandTexts.Shared} <value>`,  " +
                                                  $"`{CommandTexts.Finish}`, " +
                                                  $"`{CommandTexts.Cancel}`" +
                                                  $"."));
                if (!order.IsReadyForCompletion)
                {
                    context.Elements.Add(new PlainText("\nOrder needs 2+ participants to be completed."));
                }
            }
            else // order is closed
            {
                context.Elements.Add(new PlainText("This order may be reopened only until the next order is started."));
            }

            blocks.Add(context);

            return blocks;
        }
    }
}