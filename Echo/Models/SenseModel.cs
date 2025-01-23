namespace Echo.Models
{
    public class SenseModel
    {
        //public string Word { get; set; }
        public string ExplanationLanguageCode { get; set; }
        public string Category { get; set; }//动词or名词形容词
        public string Definition { get; set; }//一个
        public string? Description { get; set; }
        public  Dictionary<string, string>? Examples { get; set; }//// 例句 + 翻译, key为例句，value为翻译

        // 只返回最多两个例句
        public List<KeyValuePair<string, string>> LimitedExamples
        {
            get
            {
                if (Examples == null) return new List<KeyValuePair<string, string>>();
                return Examples.Take(1).ToList(); // 取前两个例句
            }
        }
    }
}
