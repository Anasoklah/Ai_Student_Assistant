namespace SyrianStudyBot.Common.Services;

public interface IPagingService
{
    (int Page, int PageSize) NormalizePaging(int page, int pageSize, int maxPageSize = 100);
}
