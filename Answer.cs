using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineSimpleQuestionnaire
{
    internal class Answer
    {
        internal int Index { get; }
        internal string Message { get; }
        internal string ReplyToken { get; }

        internal Answer(int index, string message, string replyToken)
        {
            Index = index;
            Message = message;
            ReplyToken = replyToken;
        }
    }
}
