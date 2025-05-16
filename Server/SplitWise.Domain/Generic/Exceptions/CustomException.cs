public class CustomException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; set; } = statusCode;
    public string CustomMessage { get; set; } = message;
}
