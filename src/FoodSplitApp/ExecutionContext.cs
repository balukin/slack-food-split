using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoodSplitApp.Services.Slack;
using Microsoft.WindowsAzure.Storage.Table;

namespace FoodSplitApp
{
    public class ExecutionContext
    {
        public SlackUser Caller { get; set; }

        public string TeamId { get; set; }

        // This needs to be static because scope is being lost during function entry point
        // https://github.com/Azure/azure-functions-host/issues/5098
        public static CloudTable TableStorage { get; set; }
    }
}