using System;
using System.Collections.Generic;
using System.Linq;
using FoodSplitApp.Services.Slack;
using Microsoft.WindowsAzure.Storage.Table;

namespace FoodSplitApp.Model.Orders
{
    /// <summary>
    /// Entity that fully describes and order in progress (or recently closed one)
    /// including all costs and participants.
    /// </summary>
    public class Order
    {
        public string Id { get; set; }

        public FoodUser Owner { get; set; }

        public DateTimeOffset? DateClosed { get; set; }

        public decimal SharedCost { get; set; }

        public Dictionary<string, Cost> Costs { get; set; }

        [IgnoreProperty]
        public bool IsOpen => !this.DateClosed.HasValue;

        [IgnoreProperty]
        public bool IsReadyForCompletion => IsOpen && Costs != null && Costs.Count >= 2;

        public decimal GetTotalCost()
        {
            if (Costs != null && Costs.Count > 0)
            {
                return Costs.Values.Sum(x => x.Value) + SharedCost;
            }
            else
            {
                return SharedCost;
            }
        }
    }
}