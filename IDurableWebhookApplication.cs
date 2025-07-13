using LineDC.Messaging.Webhooks;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace LineSimpleQuestionnaire
{
    public interface IDurableWebhookApplication : IWebhookApplication
    {
        ILogger Logger { get; set; }
        DurableTaskClient DurableClient { get; set; }
    }
}
