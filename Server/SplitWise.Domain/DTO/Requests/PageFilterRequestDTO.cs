namespace SplitWise.Domain.DTO.Requests;
public class PageFilterRequestDTO
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = SplitWiseConstants.PAGE_SIZE;
    public string? SearchQuery { get; set; }
    public string? SortColumn { get; set; }
    public bool IsAscending { get; set; }
}
