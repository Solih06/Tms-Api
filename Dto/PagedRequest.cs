namespace TmsApi.Dtos;

public record PagedRequest
{
    public int Page { get; init; } = 1;
    
    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > 50 ? 50 : (value < 1 ? 1 : value);
    }
    
    public string? Search { get; init; }
    public string OrderBy { get; init; } = "Title";
}