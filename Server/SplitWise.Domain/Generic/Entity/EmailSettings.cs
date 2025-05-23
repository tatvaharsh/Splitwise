namespace SplitWise.Domain.Generic.Entity;

public class EmailSettings
{
    public string EmailProvider { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;
    public string Password { get; set; } = null!;
    public int Port { get; set; }

}
