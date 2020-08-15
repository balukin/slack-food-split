using System;
using System.Threading.Tasks;
using FoodSplitApp.Model.Balance;
using FoodSplitApp.Model.Orders;
using Microsoft.WindowsAzure.Storage.Table;

namespace FoodSplitApp.Services.Storage
{
    public class AzureTableStorageProvider : IStorageProvider
    {
        private readonly ExecutionContext context;

        public AzureTableStorageProvider(ExecutionContext context)
        {
            this.context = context;
        }

        private string PartitionOrderKey => $"Order-{context.TeamId}";

        private string PartitionBalanceKey => $"Balance-{context.TeamId}";

        private const string OpenOrderRowKey = "open";

        private const string BalanceBookRowKey = "balance_book";

        public async Task CreateOrder(Order order)
        {
            await Store(CreateOrderEntity(order));
        }

        public async Task<Order> GetOrder()
        {
            var order = await Retrieve<TableEntity<Order>>(PartitionOrderKey, "open");
            return order?.Entity;
        }

        public async Task UpdateOrder(Order order)
        {
            var entity = CreateOrderEntity(order);
            var op = TableOperation.Replace(entity);
            await ExecutionContext.TableStorage.ExecuteAsync(op);
        }

        public async Task DeleteOrder()
        {
            var order = await Retrieve<TableEntity<Order>>(PartitionOrderKey, "open");

            if (order != null)
            {
                var op = TableOperation.Delete(order);
                await ExecutionContext.TableStorage.ExecuteAsync(op);
            }
        }

        public async Task<BalanceBook> GetBalanceBook()
        {
            var obj = await Retrieve<TableEntity<BalanceBook>>(PartitionBalanceKey, BalanceBookRowKey);
            if (obj != null)
            {
                return obj.Entity;
            }

            // Create new balance book if it doesn't exist yet
            var newBook = await UpsertBalanceBook(new BalanceBook());
            return newBook;
        }

        public async Task<BalanceBook> UpsertBalanceBook(BalanceBook balanceBook)
        {
            var entity = CreateBalanceBookEntity(balanceBook);
            await Store(entity);

            var updatedBook = await GetBalanceBook();
            return updatedBook;
        }

        private TableEntity<Order> CreateOrderEntity(Order order)
        {
            var entity = new TableEntity<Order>(order)
            {
                PartitionKey = PartitionOrderKey,
                RowKey = OpenOrderRowKey,
                Timestamp = DateTimeOffset.UtcNow,
                ETag = "*"
            };

            return entity;
        }

        private TableEntity<BalanceBook> CreateBalanceBookEntity(BalanceBook book)
        {
            var entity = new TableEntity<BalanceBook>(book)
            {
                PartitionKey = PartitionBalanceKey,
                RowKey = BalanceBookRowKey,
                Timestamp = DateTimeOffset.UtcNow,
                ETag = "*"
            };

            return entity;
        }

        private async Task Store(ITableEntity value)
        {
            var op = TableOperation.InsertOrReplace(value);
            await ExecutionContext.TableStorage.ExecuteAsync(op);
        }

        private async Task<T> Retrieve<T>(string partitionKey, string rowKey) where T : ITableEntity
        {
            var op = TableOperation.Retrieve<T>(partitionKey, rowKey);
            var tableResult = await ExecutionContext.TableStorage.ExecuteAsync(op);
            return (T)tableResult.Result;
        }
    }
}