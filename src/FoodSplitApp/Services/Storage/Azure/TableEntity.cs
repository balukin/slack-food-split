using System.Text.Json;
using Microsoft.WindowsAzure.Storage.Table;

namespace FoodSplitApp.Services.Storage
{
    public class TableEntity<T> : TableEntity
    {
        [IgnoreProperty]
        public T Entity
        {
            get => JsonSerializer.Deserialize<T>(Json);
            set => Json = JsonSerializer.Serialize(value);
        }

        public string Json { get; set; }

        public TableEntity(T entity)
        {
            Entity = entity;
        }

        public TableEntity()
        {
        }
    }
}