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

        private List<(string question, string[] reply)> _enq = new List<(string, string[])>
        {
            ($"Q1. {Environment.GetEnvironmentVariable("QUESTION1")}", new [] { $"①{Environment.GetEnvironmentVariable("OPTION1_1")}", $"②{Environment.GetEnvironmentVariable("OPTION1_2")}" }),
        };

        protected override async Task OnMessageAsync(MessageEvent e)
        {
            if (e.Message is TextEventMessage textMessage)
            {
                var status = await DurableClient.GetInstanceAsync(e.Source.UserId, getInputsAndOutputs: true);
                var index = (status?.ReadCustomStatusAs<QuestionIndex>() ?? new QuestionIndex { Index = -1 }).Index + 1;
                Logger.LogInformation($"OnMessageAsync - index: {index}");

                if (_enq.Count == index + 1)
                {
                    await DurableClient.RaiseEventAsync(
                        e.Source.UserId,
                        "answer",
                        JsonSerializer.Serialize(new Answer
                        {
                            Index = -1,
                            Message = textMessage.Text,
                            ReplyToken = e.ReplyToken
                        }));
                }

                if (status?.RuntimeStatus == OrchestrationRuntimeStatus.Pending
                    || status?.RuntimeStatus == OrchestrationRuntimeStatus.Running)
                {
                    await DurableClient.RaiseEventAsync(
                        e.Source.UserId,
                        "answer",
                        JsonSerializer.Serialize(new Answer
                        {
                            Index = index,
                            Message = textMessage.Text,
                            ReplyToken = e.ReplyToken
                        }));
                }
                else
                {
                    await Client.ReplyMessageAsync(
                        e.ReplyToken,
                        "リッチメニューの右上を押してね");
                }
            }
            else
            {
                await Client.ReplyMessageAsync(
                    e.ReplyToken,
                    "リッチメニューの右上を押してね");
            }
        }

        protected override async Task OnPostbackAsync(PostbackEvent e)
        {
            switch (e.Postback.Data)
            {
                case "start":
                    await DurableClient.PurgeInstanceAsync(e.Source.UserId);
                    await DurableClient.ScheduleNewOrchestrationInstanceAsync(
                        nameof(LineSimpleQuestionnaire),
                        input: new List<string>(),
                        options: new StartOrchestrationOptions
                        {
                            InstanceId = e.Source.UserId
                        });

                    await ReplyNextQuestionAsync(e.ReplyToken, 0);
                    break;
                case "send":
                    await Client.ReplyMessageAsync(
                        e.ReplyToken,
                        Environment.GetEnvironmentVariable("THANKS1"),
                        Environment.GetEnvironmentVariable("THANKS2").Replace("\\n", "\n"));
                    await DurableClient.PurgeInstanceAsync(e.Source.UserId);
                    break;
                case "cancel":
                    await Client.ReplyMessageAsync(
                        e.ReplyToken,
                        "回答をキャンセルしました。もう一度回答する場合はリッチメニューの右上を押してね");
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
                    FlexMessage
                    .CreateBubbleMessage("設問")
                    .SetBubbleContainer(
                        new BubbleContainer()
                        .SetHeader(BoxLayout.Horizontal)
                        .AddHeaderContents(
                            new TextComponent
                            {
                                Text = next.question,
                                Align = Align.Center,
                                Weight = Weight.Bold,
                                Color = ColorCode.LightGray,
                            })
                        .SetBody(
                            new BoxComponent
                            {
                                Layout = BoxLayout.Vertical,
                                Contents =
                                    next.reply
                                    .Select(r => new ButtonComponent
                                    {
                                        Action = new MessageTemplateAction(r, r)
                                    }).ToArray()

                            }))
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
