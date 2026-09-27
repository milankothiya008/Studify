namespace SmartLearningPlatform.Repositories;

/// <summary>
/// The page counters without the element type, so the shared <c>_Pager</c> view
/// can bind to a page of any entity rather than needing one partial each.
/// </summary>
public interface IPagedResultInfo
{
    int PageNumber { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
    int FirstItemOnPage { get; }
    int LastItemOnPage { get; }
}
