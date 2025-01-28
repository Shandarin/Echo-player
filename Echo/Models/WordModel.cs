
using CommunityToolkit.Mvvm.ComponentModel;

namespace Echo.Models
{
    public class WordModel: ObservableObject
    {
        public string Word { get; set; }//or phrase headword    //from api
        public string SourceLanguageCode { get; set; }//原语言
        public string TargetLanguageCode { get; set; }//翻译的语言
        //public string TranslatedText { get; set; }
        public List<PronunciationModel>? Pronounciations { get; set; }          //from api
        public  Dictionary<string, string>? Definitions { get; set; }//按照词性分类的所有释义, v:def1,def2       //from api
        public  List<string>? Inflections { get; set; }//from api
        public List<string>? Synonyms { get; set; }//from api
        public List<SenseModel> Senses {  get; set; }//释义，按原有结构, v:def1,v:def2  //from api
        public List<SenseModel> OriginalSenses { get; set; }//原文释义, 按原有结构, v:def1,v:def2   //from api
        public Dictionary<string, string>? OriginalDefinitions { get; set; }//原文释义, v:def1,def2   //from api
        public Dictionary<string, string>? Phrases { get; set; }//短语+翻译   //from api
        public bool IsFavorite { get; set; }
        public bool IsSuccess { get; set; }//是否成功获取到数据
        public string SourceFilePath { get; set; }
        public string SourceFileName { get; set; }
        public long SourceStartTime { get; set; }
        public long SourceEndTime { get; set; }
        public long Id { get; set; }    //from database
        //public string CollectionName { get; set; }
        public long CollectionId { get; set; }     //from database
        public List<SenseGroup> GroupedSenses
        {
            get
            {
                if (Senses == null || Senses.Count == 0)
                    return new List<SenseGroup>();

                return Senses
                    .GroupBy(s => new { s.ExplanationLanguageCode, s.Category })
                    .Select(g => new SenseGroup
                    {
                        ExplanationLanguageCode = g.Key.ExplanationLanguageCode,
                        Category = g.Key.Category,
                        Senses = g.ToList()
                    })
                    .ToList();
            }
        }
    }
}
