namespace SyrianStudyBot.Infrastructure.Common;

public class PagingService : IPagingService
{
    public (int Page, int PageSize) NormalizePaging(int page, int pageSize, int maxPageSize = 100)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, maxPageSize);
        return (page, pageSize);
    }
}
