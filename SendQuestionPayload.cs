using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LineSimpleQuestionnaire
{
    [JsonSerializable(typeof(SendQuestionPayload))]
    public class SendQuestionPayload
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }
        [JsonPropertyName("replyToken")]
        public string ReplyToken { get; set; }
    }
}
