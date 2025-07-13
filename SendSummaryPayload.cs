using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LineSimpleQuestionnaire
{
    public class SendSummaryPayload
    {
        [JsonPropertyName("replyToken")]
        public string ReplyToken { get; set; }
        [JsonPropertyName("answers")]
        public List<string> Answers { get; set; }
    }
}
