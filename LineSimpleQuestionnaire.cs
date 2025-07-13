using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
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

        var value = await context.WaitForExternalEvent<(int index, string message, string replyToken)>("answer");
        logger.LogInformation($"Orchestrator - index: {value.index}");
        throw new Exception(JsonSerializer.Serialize(value));

        answers.Add(value.message);

        if (value.index == -1)
        {
            await context.CallActivityAsync(
                nameof(SendSummaryActivity),
                (value.replyToken, answers));
        }
        else
        {
            context.SetCustomStatus(value.index);

            await context.CallActivityAsync(
                nameof(SendQuestionActivity),
                (value.replyToken, value.index + 1));

            context.ContinueAsNew(answers);
        }
    }

    [Function(nameof(SendQuestionActivity))]
    public async Task SendQuestionActivity(
        [ActivityTrigger] (string replyToken, int index) input)
    {
        await _app.ReplyNextQuestionAsync(
            input.replyToken,
            input.index);
    }
    
    public async Task SendSummaryActivity(
        [ActivityTrigger] (string replyToken, List<string> answers) input)
    {
        await _app.ReplySummaryAsync(
            input.replyToken,
            input.answers);
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