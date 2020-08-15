using System.Threading.Tasks;
using FoodSplitApp.Model.Balance;
using FoodSplitApp.Model.Orders;

namespace FoodSplitApp.Services.Storage
{
    /// <summary>
    /// Basic storage functions to manipulate order and balance book entities
    /// </summary>
    public interface IStorageProvider
    {
        Task CreateOrder(Order order);

        Task<Order> GetOrder();

        Task UpdateOrder(Order order);

        Task DeleteOrder();

        Task<BalanceBook> GetBalanceBook();

        Task<BalanceBook> UpsertBalanceBook(BalanceBook balanceBook);
    }
}