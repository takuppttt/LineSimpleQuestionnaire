using LineDC.Messaging.Messages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;

namespace LineSimpleQuestionnaire;

public class LineSimpleQuestionnaire
{
    private EnqBotApp _app { get; }

    public LineSimpleQuestionnaire(
        EnqBotApp app)
    {
        _app = app;
    }

    [Function(nameof(LineSimpleQuestionnaire))]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(LineSimpleQuestionnaire));
        
        var answers = context.GetInput<List<string>>() ?? new List<string>();

        var value = JsonSerializer.Deserialize<Answer>(await context.WaitForExternalEvent<string>("answer"));
        logger.LogInformation($"Orchestrator - index: {value.Index}");

        answers.Add(value.Message);

        if (value.Index == -1)
        {
            await context.CallActivityAsync(
                nameof(SendSummaryActivity),
                JsonSerializer.Serialize(
                    new SendSummaryPayload
                    {
                        ReplyToken = value.ReplyToken,
                        Answers = answers
                    }));
        }
        else
        {
            context.SetCustomStatus($"{value.Index}");

            await context.CallActivityAsync(
                nameof(SendQuestionActivity),
                JsonSerializer.Serialize(
                    new SendQuestionPayload
                    {
                        Index = value.Index + 1,
                        ReplyToken = value.ReplyToken
                    }));

            context.ContinueAsNew(answers);
        }
    }

    [Function(nameof(SendQuestionActivity))]
    public async Task SendQuestionActivity(
        [ActivityTrigger] string input)
    {
        var payload = JsonSerializer.Deserialize<SendQuestionPayload>(input);
        await _app.ReplyNextQuestionAsync(
            payload.ReplyToken,
            payload.Index);
    }
    
    public async Task SendSummaryActivity(
        [ActivityTrigger] string input)
    {
        var payload = JsonSerializer.Deserialize<SendSummaryPayload>(input);
        await _app.ReplySummaryAsync(
            payload.ReplyToken,
            payload.Answers);
    }

    [Function(nameof(HttpStart))]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(HttpStart));

        _app.Logger = logger;
        _app.DurableClient = client;

        await _app.RunAsync(
            req.Headers.GetValues("x-line-signature").First(),
            await req.ReadAsStringAsync());

        return req.CreateResponse(HttpStatusCode.OK);
    }
}