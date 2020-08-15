using System.Text.Json.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

namespace FoodSplitApp.Model.Orders
{
    /// <summary>
    /// Entity that describes singular cost item from an order, set by some user.
    /// </summary>
    public class Cost
    {
        // Human-readable
        public string DebtorName { get; set; }

        // Uniquely identifiable
        public string DebtorId { get; set; }

        public decimal Value { get; set; }

        public string Item { get; set; }

        /// <summary>
        /// Markdown representation of this item including value and owner.
        /// </summary>
        [IgnoreProperty]
        [JsonIgnore]
        public string MarkdownDescription
        {
            get
            {
                var itemStr = Item != null ? $" ({Item})" : "";
                var debtorStr = $"<@{DebtorId}>";
                return $"> • {debtorStr}: *{Value}*{itemStr}";
            }
        }
    }
}