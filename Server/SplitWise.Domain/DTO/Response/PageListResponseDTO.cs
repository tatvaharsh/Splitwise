namespace SplitWise.Domain.DTO.Response;

public class PageListResponseDTO<T> where T : class
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<T> Data { get; set; } = [];
}
