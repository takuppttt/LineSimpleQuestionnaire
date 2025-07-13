using LineDC.Messaging;
using LineSimpleQuestionnaire;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddSingleton<ILineMessagingClient>(_ =>
        LineMessagingClient.Create(new HttpClient(), Environment.GetEnvironmentVariable("LINE_ACCESS_TOKEN")))
    .AddTransient(s =>
        new EnqBotApp(s.GetRequiredService<ILineMessagingClient>(), Environment.GetEnvironmentVariable("LINE_CHANNEL_SECRET")));

builder.Build().Run();
