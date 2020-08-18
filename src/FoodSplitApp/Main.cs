using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SlackNet.AspNetCore;

namespace FoodSplitApp
{
    public class Main
    {
        private readonly ISlackRequestHandler requestHandler;
        private readonly SlackEndpointConfiguration endpointConfig;

        public Main(ISlackRequestHandler requestHandler, SlackEndpointConfiguration endpointConfig, ExecutionContext context)
        {
            this.requestHandler = requestHandler;
            this.endpointConfig = endpointConfig;
        }

        /// <summary>
        /// HTTP Entry point for all incoming commands.
        /// </summary>
        [FunctionName("FoodCommand")]
        public async Task<IActionResult> Command(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "food/command")] HttpRequest req,
            [Table("FoodTable")] CloudTable table)
        {
            ExecutionContext.TableStorage = table;
            await requestHandler.HandleSlashCommandRequest(req, endpointConfig);

            return new OkResult();
        }
    }
}