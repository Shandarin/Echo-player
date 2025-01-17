namespace Echo.Models
{
    public class SenseModel
    {
        //public string Word { get; set; }
        public string Category { get; set; }//动词or名词形容词
        public string Definition { get; set; }//一个
        public string? Description { get; set; }
        public  Dictionary<string, string>? Examples { get; set; }//例句+翻译
    }
}
