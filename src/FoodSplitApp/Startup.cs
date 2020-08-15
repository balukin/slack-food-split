using System;
using FoodSplitApp;
using FoodSplitApp.Services;
using FoodSplitApp.Services.Slack;
using FoodSplitApp.Services.Storage;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlackNet.AspNetCore;
using SlackNet.Events;
using SlackNet.Interaction;

[assembly: FunctionsStartup(typeof(Startup))]

namespace FoodSplitApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var accessToken = Environment.GetEnvironmentVariable("SlackAccessToken", EnvironmentVariableTarget.Process);

            builder.Services.AddLogging();
            builder.Services.AddSlackNet(c => c
                .UseApiToken(accessToken)
                .RegisterSlashCommandHandler<SlackCommandHandler>("/food"));

            builder.Services.AddSingleton(new SlackEndpointConfiguration());

            builder.Services.AddScoped<IFoodService, FoodService>();
            builder.Services.AddScoped<IStorageProvider, AzureTableStorageProvider>();
            builder.Services.AddScoped<ExecutionContext>();
        }
    }
}