

namespace Echo.Models
{
    class SentenceModel
    {
        string Sentence { get; set; }
        string Translation { get; set; }
        string Language { get; set; }
        List<WordModel> Words { get; set; }
        string Analysis { get; set; }
    }
}
