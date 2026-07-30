namespace SyrianStudyBot.Application.Documents.Dtos;

/// <summary>
/// One chapter or section found in a document table of contents.
/// </summary>
public class BookStructureEntryDto
{
    public string Title { get; init; } = string.Empty;
    public int? PageNumber { get; init; }
    public string Level { get; init; } = "Section";
    public string? ParentChapter { get; init; }
}

/// <summary>
/// Extracted table-of-contents structure used to relate chunks to chapters.
/// </summary>
public class BookStructureDto
{
    public List<BookStructureEntryDto> Chapters { get; init; } = [];
    public List<BookStructureEntryDto> Sections { get; init; } = [];
}
