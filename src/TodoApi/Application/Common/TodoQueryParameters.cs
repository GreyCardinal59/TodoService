namespace TodoApi.Application.Common;

public class TodoQueryParameters
{
    public string? Title { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
