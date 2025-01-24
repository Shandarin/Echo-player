
using CommunityToolkit.Mvvm.ComponentModel;
//for words preview
namespace Echo.Models
{
    public class WordBasicModel: ObservableObject
    {
        public string Word { get; set; }//or phrase
        public string SourceLanguageCode { get; set; }//原语言
        public string TargetLanguageCode { get; set; }//翻译的语
        public bool IsFavorite { get; set; }
        public bool IsSuccess { get; set; }//是否成功获取到数据
        public long Id { get; set; }
        public long CollectionId { get; set; }
    }
}
