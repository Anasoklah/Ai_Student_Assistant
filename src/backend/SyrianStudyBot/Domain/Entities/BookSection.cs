namespace SyrianStudyBot.Domain.Entities;

public class BookSection
{
    public Guid Id { get; set; }
    public Guid ChapterId { get; set; }
    public int SectionNumber { get; set; }
    public string Title { get; set; }= string.Empty;
    public string NormalizedTitle { get; set; } = string.Empty;
    public int? StartPage { get; set; }
    public int? EndPage { get; set; }
    public BookChapter Chapter { get; set; } = null!;
}

