

namespace Echo.Models
{
    public class SentenceModel
    {
        public long Id { get; set; }
        public string Sentence { get; set; }
        public string Translation { get; set; }
        public string TargetLanguageCode { get; set; }
        public string SourceLanguageCode { get; set; }
        public long CollectionId { get; set; }
        //List<WordModel> Words { get; set; }
        //string Analysis { get; set; }
    }
}
