namespace SmartLearningPlatform.Repositories;

/// <summary>
/// One page of rows plus the counters a pager needs. Stands in for Spring
/// Data's <c>Page&lt;T&gt;</c>.
/// </summary>
public class PagedResult<T> : IPagedResultInfo
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>1-based.</summary>
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Rows matching the query across every page.</summary>
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;

    /// <summary>1-based index of the first row on this page, 0 when empty.</summary>
    public int FirstItemOnPage => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

    public int LastItemOnPage => Math.Min(PageNumber * PageSize, TotalCount);

    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 20) =>
        new() { Items = Array.Empty<T>(), PageNumber = pageNumber, PageSize = pageSize, TotalCount = 0 };
}
