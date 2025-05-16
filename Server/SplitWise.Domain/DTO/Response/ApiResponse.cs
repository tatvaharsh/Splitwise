namespace SplitWise.Domain.DTO.Response;

public record ApiResponse<T>(int StatusCode, string Message, T? Content = null) where T : class
{
    public bool Success { get; init; } = true;
}