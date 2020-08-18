using System;
using System.Threading.Tasks;
using FoodSplitApp.Model;
using FoodSplitApp.Model.Balance;
using FoodSplitApp.Model.Orders;
using FoodSplitApp.Services.Slack;
using Microsoft.Build.Utilities;
using Task = System.Threading.Tasks.Task;

namespace FoodSplitApp.Services
{
    public interface IFoodService
    {
        Task<Order> OpenNewOrder(SlackUser host);

        Task<Order> GetOpenOrder();

        /// <summary>
        /// Finishes order and returns update balance book and the finished order.
        /// </summary>
        /// <param name="orderId">Order id to be finished.</param>
        Task<(Order finishedOrder, BalanceBook updatedBalance)> FinishOrder();

        Task CancelOpenOrder(FoodUser caller);

        Task<(Order reopenedOrder, BalanceBook updatedBalance)> ReopenOrder();

        Task<Order> AddEater(FoodUser eater, decimal cost, string item);

        Task<Order> SetSharedCost(decimal value);

        Task<PairBalance> OweCredit(FoodUser debtor, FoodUser creditor, decimal creditValue);

        Task<PairBalance> ResetCredit(FoodUser debtor, FoodUser creditor);

        Task<BalanceBook> GetBalanceBook();
    }
}