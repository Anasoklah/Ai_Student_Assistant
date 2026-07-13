namespace SyrianStudyBot.Application.Common;

/// <summary>
/// Generic paginated result for entity queries. Used internally by repositories to
/// return query results with pagination metadata. Consumed by UseCases and mapped
/// to DTOs.
/// </summary>
/// <typeparam name="T">The entity type being paginated.</typeparam>
public record EntityPage<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public bool HasNextPage => Page * PageSize < TotalCount;
    public bool HasPreviousPage => Page > 1;
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Transforms the entity list into a different type using the provided mapper.
    /// Useful for converting entities to DTOs at the repository boundary.
    /// </summary>
    public EntityPage<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return new EntityPage<TResult>(Items.Select(mapper).ToList(), TotalCount, Page, PageSize);
    }
}
