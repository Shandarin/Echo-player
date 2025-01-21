﻿
namespace Echo.Models
{
    public class WordModel
    {
        public string Word { get; set; }//or phrase
        public string LanguageCode { get; set; }
        //public string TranslatedText { get; set; }
        public List<PronunciationModel>? Pronounciations { get; set; }
        public  Dictionary<string, string>? Definitions { get; set; }//按照词性分类的所有释义, v:def1,def2
        public  List<string>? Inflections { get; set; }
        public List<string>? Synonyms { get; set; }
        public List<SenseModel> Senses {  get; set; }//释义，按原有结构, v:def1,v:def2
        public List<SenseModel> OriginalSenses { get; set; }//原文释义, 按原有结构, v:def1,v:def2
        public Dictionary<string, string>? OriginalDefinitions { get; set; }//原文释义, v:def1,def2
        public Dictionary<string, string>? Phrases { get; set; }//短语+翻译
        public bool IsFavorite { get; set; }
        public bool IsSuccess { get; set; }//是否成功获取到数据
        public string SourceFileName { get; set; }
        public long SourceStartTime { get; set; }
        public long SourceEndTime { get; set; }
    }
}
