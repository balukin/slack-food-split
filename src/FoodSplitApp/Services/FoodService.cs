using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoodSplitApp.Errors;
using FoodSplitApp.Model;
using FoodSplitApp.Model.Balance;
using FoodSplitApp.Model.Orders;
using FoodSplitApp.Services.Slack;
using FoodSplitApp.Services.Storage;
using Microsoft.Build.Utilities;
using Task = System.Threading.Tasks.Task;

namespace FoodSplitApp.Services
{
    public class FoodService : IFoodService
    {
        private readonly IStorageProvider storage;

        public FoodService(IStorageProvider storage)
        {
            this.storage = storage;
        }

        public async Task<Order> OpenNewOrder(SlackUser host)
        {
            var order = new Order()
            {
                Owner = new FoodUser(host),
                Costs = new Dictionary<string, Cost>(),
                Id = Guid.NewGuid().ToString(),
                DateCreated = DateTimeOffset.UtcNow
            };

            var currentOrder = await storage.GetOrder();
            if (currentOrder != null && currentOrder.IsOpen)
            {
                throw new BadRequestException("There is already an open order. Cancel it first.");
            }

            await storage.CreateOrder(order);
            return order;
        }

        public async Task<Order> GetOpenOrder()
        {
            return await storage.GetOrder();
        }

        public async Task<Order> AddEater(FoodUser eater, decimal cost, string item)
        {
            var order = await storage.GetOrder();
            if (order == null)
            {
                throw new BadRequestException("There is no order open.");
            }

            if (!order.IsOpen)
            {
                throw new BadRequestException("Order is closed and cannot be altered.");
            }

            order.Costs[eater.UniqueId] = new Cost
            {
                DebtorId = eater.UniqueId,
                DebtorName = eater.FriendlyName,
                Value = cost,
                Item = item
            };

            if (order.Costs[eater.UniqueId].Value == 0)
            {
                order.Costs.Remove(eater.UniqueId);
            }

            await storage.UpdateOrder(order);
            return order;
        }

        public async Task<Order> SetSharedCost(decimal value)
        {
            var order = await storage.GetOrder();
            if (order == null)
            {
                throw new BadRequestException("There is no order open.");
            }

            if (!order.IsOpen)
            {
                throw new BadRequestException("Order is closed and cannot be altered.");
            }

            order.SharedCost = value;
            await storage.UpdateOrder(order);
            return order;
        }

        public async Task<PairBalance> OweCredit(FoodUser debtor, FoodUser creditor, decimal value)
        {
            var balanceBook = await storage.GetBalanceBook();
            balanceBook.AddDebt(debtor, creditor, value);
            await storage.UpsertBalanceBook(balanceBook);
            return balanceBook.GetBalance(debtor, creditor);
        }

        public async Task<PairBalance> ResetCredit(FoodUser debtor, FoodUser creditor)
        {
            var balanceBook = await storage.GetBalanceBook();
            var pair = balanceBook.GetBalance(debtor, creditor);
            if (pair.HasPositiveBalance(creditor))
            {
                pair.AddDebt(creditor, pair.Balance);
                await storage.UpsertBalanceBook(balanceBook);
                return pair;
            }
            else
            {
                throw new BadRequestException("Given user doesn't owe you anything.");
            }
        }

        public async Task<BalanceBook> GetBalanceBook()
        {
            return await storage.GetBalanceBook();
        }

        public async Task CancelOpenOrder(FoodUser caller)

        {
            var order = await storage.GetOrder();
            if (order == null || !order.IsOpen)
            {
                throw new BadRequestException("There is no valid order to cancel.");
            }

            const int minTimeout = 30;
            if (caller.UniqueId != order.Owner.UniqueId &&
                DateTimeOffset.UtcNow - order.DateCreated < TimeSpan.FromMinutes(minTimeout))
            {
                throw new BadRequestException($"Only order owner can cancel the order during first {minTimeout} minutes.");
            }

            await storage.DeleteOrder();
        }

        /// <inheritdoc />
        public async Task<(Order finishedOrder, BalanceBook updatedBalance)> FinishOrder()
        {
            var order = await storage.GetOrder();

            if (order.Costs.Count < 2)
            {
                throw new BadRequestException("Order needs to have at least 2 participant to be completed.");
            }

            if (order.DateClosed.HasValue)
            {
                throw new BadRequestException("This order has already been finalized.");
            }

            var balanceBook = await storage.GetBalanceBook();
            var sharedPart = order.SharedCost / order.Costs.Count;

            foreach (var cost in order.Costs.Values.Where(cost => cost.DebtorId != order.Owner.UniqueId))
            {
                var eater = new FoodUser(cost.DebtorId, cost.DebtorName);
                var addedDebt = cost.Value + sharedPart;

                balanceBook.AddDebt(eater, order.Owner, addedDebt);
            }

            order.DateClosed = DateTimeOffset.UtcNow;

            // Maybe add transactions here?
            await storage.UpsertBalanceBook(balanceBook);
            await storage.UpdateOrder(order);

            return (order, balanceBook);
        }

        public async Task<(Order reopenedOrder, BalanceBook updatedBalance)> ReopenOrder()
        {
            var order = await storage.GetOrder();
            if (order?.DateClosed == null)
            {
                throw new BadRequestException("There is no valid order to reopen.");
            }

            var balanceBook = await storage.GetBalanceBook();
            var sharedPart = order.SharedCost / order.Costs.Count;

            foreach (var cost in order.Costs.Values.Where(cost => cost.DebtorId != order.Owner.UniqueId))
            {
                var eater = new FoodUser(cost.DebtorId, cost.DebtorName);
                var addedDebt = cost.Value + sharedPart;

                balanceBook.AddDebt(order.Owner, eater, addedDebt);
            }

            order.DateClosed = null;

            await storage.UpdateOrder(order);
            await storage.UpsertBalanceBook(balanceBook);

            return (order, balanceBook);
        }
    }
}