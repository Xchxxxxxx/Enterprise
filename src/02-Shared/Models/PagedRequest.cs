namespace EfCore.Enterprise.Shared.Models;

public class PagedRequest
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortField { get; set; }
    public bool IsAscending { get; set; } = true;

    public int Skip => (PageIndex - 1) * PageSize;
    public int Take => PageSize;
}

public class PagedRequest<TFilter> : PagedRequest
{
    public TFilter? Filter { get; set; }
}