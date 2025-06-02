namespace SplitWise.Domain.Generic.Entity;

public class JwtSetting
{
    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int AccessTokenExpiryMinutes { get; set; }
    public int RefreshTokenExpiryDay { get; set; }
}