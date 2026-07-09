

namespace SyrianStudyBot.Domain.Entities;

public class BookChapter
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChapterNumber { get; set; }         
    public string Title { get; set; } = string.Empty;              
    public string NormalizedTitle { get; set; } = string.Empty;
    public int? StartPage { get; set; }
    public int? EndPage { get; set; }
    public Document Document { get; set; } = null!;
    public List<BookSection> Sections { get; set; } = [];
}
