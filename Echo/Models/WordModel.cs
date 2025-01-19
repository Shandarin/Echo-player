
namespace Echo.Models
{
    public class WordModel
    {
        public string Word { get; set; }//or phrase
        //public string TranslatedText { get; set; }
        public List<PronunciationModel>? Pronounciations { get; set; }
        public  Dictionary<string, string>? Definitions { get; set; }//按照词性分类的所有释义, v:def1,def2
        public  Dictionary<string, string>? Inflections { get; set; }
        public List<string>? Synonyms { get; set; }
        public List<SenseModel> Senses {  get; set; }
        public Dictionary<string, string>? OriginalDefinitions { get; set; }//原文释义, v:def1,def2
        public Dictionary<string, string>? Phrases { get; set; }//短语+翻译
        public bool IsFavorite { get; set; }
        public bool IsSuccess { get; set; }//是否成功获取到数据
    }
}
