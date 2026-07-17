namespace ImageShare.Browsing;

public sealed class PaginatedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }

    public static PaginatedResult<T> Paginate(IReadOnlyList<T> items, int page, int pageSize)
    {
        var paged = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<T>
        {
            Items = paged,
            Page = page,
            PageSize = pageSize,
            TotalCount = items.Count,
        };
    }
}
