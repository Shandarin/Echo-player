using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo.Models
{
    public class SenseGroup
    {
        public string ExplanationLanguageCode { get; set; }
        public string Category { get; set; }
        public List<SenseModel> Senses { get; set; } = new();
    }
}
