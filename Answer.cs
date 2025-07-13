using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LineSimpleQuestionnaire
{
    [JsonSerializable(typeof(Answer))]
    internal class Answer
    {
        [JsonPropertyName("index")]
        internal int Index { get; set; }
        [JsonPropertyName("message")]
        internal string Message { get; set; }
        [JsonPropertyName("replyToken")]
        internal string ReplyToken { get; set; }

        internal Answer(int index, string message, string replyToken)
        {
            Index = index;
            Message = message;
            ReplyToken = replyToken;
        }
    }
}
