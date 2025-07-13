using LineDC.Messaging;
using LineDC.Messaging.Messages;
using LineDC.Messaging.Messages.Actions;
using LineDC.Messaging.Messages.Flex;
using LineDC.Messaging.Webhooks;
using LineDC.Messaging.Webhooks.Events;
using LineDC.Messaging.Webhooks.Messages;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LineSimpleQuestionnaire
{
    public class EnqBotApp : WebhookApplication, IDurableWebhookApplication
    {
        public ILogger Logger { get; set; }
        public DurableTaskClient DurableClient { get; set; }

        public EnqBotApp(
            ILineMessagingClient client,
            string channelSecret)
            : base(client, channelSecret)
        {
        }

        private List<(string question, string[] quickReply)> _enq = new List<(string, string[])>
        {
            ("Azure は好きですか？", new [] { "はい", "Yes" }),
            ("Azure Functions は好きですか？", new [] { "はい", "もちろん", "大好きです" }),
            ("Web Apps は？", new [] { "好きです", "大好きです" }),
            ("Azure で好きなサービスは？", null)
        };

        protected override async Task OnMessageAsync(MessageEvent e)
        {
            if (e.Message is TextEventMessage textMessage)
            {
                if (textMessage.Text == "アンケート開始")
                {
                    await DurableClient.PurgeInstanceAsync(e.Source.UserId);
                    await DurableClient.ScheduleNewOrchestrationInstanceAsync(
                        nameof(LineSimpleQuestionnaire),
                        input: new List<string>(),
                        options: new StartOrchestrationOptions
                        {
                            InstanceId = e.Source.UserId
                        });

                    await ReplyNextQuestionAsync(e.ReplyToken, 0);
                }
                else
                {
                    var status = await DurableClient.GetInstanceAsync(e.Source.UserId);
                    await Client.ReplyMessageAsync(
                        e.ReplyToken,
                        JsonSerializer.Serialize(status));
                    var index = (status?.ReadCustomStatusAs<int>() ?? -1) + 1;
                    Logger.LogInformation($"OnMessageAsync - index: {index}");

                    if (_enq.Count == index + 1)
                    {
                        await DurableClient.RaiseEventAsync(
                            e.Source.UserId,
                            "answer",
                            (-1, textMessage.Text, e.ReplyToken));
                    }

                    if (status?.RuntimeStatus == OrchestrationRuntimeStatus.Pending
                        || status?.RuntimeStatus == OrchestrationRuntimeStatus.Running)
                    {
                        await DurableClient.RaiseEventAsync(
                            e.Source.UserId,
                            "answer",
                            (index, textMessage.Text, e.ReplyToken));
                    }
                    else
                    {
                        await Client.ReplyMessageAsync(
                            e.ReplyToken,
                            "「アンケート開始」と送ってね");
                    }
                }
            }
            else
            {
                await Client.ReplyMessageAsync(
                    e.ReplyToken,
                    "「アンケート開始」と送ってね");
            }
        }

        protected override async Task OnPostbackAsync(PostbackEvent e)
        {
            switch (e.Postback.Data)
            {
                case "send":
                    await Client.ReplyMessageAsync(
                        e.ReplyToken,
                        "回答ありがとうございました。");
                    await DurableClient.PurgeInstanceAsync(e.Source.UserId);
                    break;
                case "cancel":
                    await Client.ReplyMessageAsync(
                        e.ReplyToken,
                        "回答をキャンセルしました。もう一度回答する場合は「アンケート開始と送ってください。」");
                    await DurableClient.PurgeInstanceAsync(e.Source.UserId);
                    break;
            }
        }

        public async Task ReplyNextQuestionAsync(
            string replyToken,
            int index)
        {
            var next = _enq[index];

            await Client.ReplyMessageAsync(
                replyToken,
                new List<ISendMessage>
                {
                    next.quickReply != null
                    ? new TextMessage(
                        next.question,
                        new QuickReply(next.quickReply.Select(
                            q => new QuickReplyButtonObject(new MessageTemplateAction(q, q))).ToList()))
                    : new TextMessage(next.question)
                });
        }

        public async Task ReplySummaryAsync(
            string replyToken,
            List<string> answers)
        {
            await Client.ReplyMessageAsync(
                replyToken,
                new List<ISendMessage>
                {
                    FlexMessage
                    .CreateBubbleMessage("確認")
                    .SetBubbleContainer(
                        new BubbleContainer()
                        .SetHeader(BoxLayout.Horizontal)
                        .AddHeaderContents(
                            new TextComponent
                            {
                                Text = "以下の内容でよろしいですか？",
                                Align = Align.Center,
                                Weight = Weight.Bold,
                            })
                        .SetBody(
                            new BoxComponent
                            {
                                Layout = BoxLayout.Vertical,
                                Contents =
                                    _enq.Zip(answers, (enq, answer) => (enq.question, answer))
                                    .Select(p => new BoxComponent
                                    {
                                        Layout = BoxLayout.Vertical,
                                        Contents = new IFlexComponent[]
                                        {
                                            new TextComponent
                                            {
                                                Text = p.question,
                                                Size = ComponentSize.Xs,
                                                Align = Align.Start,
                                                Weight = Weight.Bold,
                                            },
                                            new TextComponent
                                            {
                                                Text = p.answer,
                                                Align = Align.Start,
                                            }
                                        }
                                    }).ToArray()
                                
                            })
                        .SetFooter(
                            new BoxComponent
                            {
                                Layout = BoxLayout.Horizontal,
                                Contents = new IFlexComponent[]
                                {
                                    new ButtonComponent
                                    {
                                        Action = new PostbackTemplateAction(
                                            "送信する",
                                            "send")
                                    },
                                    new ButtonComponent
                                    {
                                        Action = new PostbackTemplateAction(
                                            "やり直す",
                                            "cancel")
                                    }
                                }
                            }))
                });
        }
    }
}
