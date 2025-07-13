using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LineSimpleQuestionnaire
{
    [JsonSerializable(typeof(Answer))]
    public class Answer
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("replyToken")]
        public string ReplyToken { get; set; }
    }
}
