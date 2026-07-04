namespace SyrianStudyBot.Infrastructure.Common;

public interface IPagingService
{
    (int Page, int PageSize) NormalizePaging(int page, int pageSize, int maxPageSize = 100);
}
