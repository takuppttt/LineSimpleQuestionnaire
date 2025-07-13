using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LineSimpleQuestionnaire
{
    [JsonSerializable(typeof(QuestionIndex))]
    public class QuestionIndex
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}
