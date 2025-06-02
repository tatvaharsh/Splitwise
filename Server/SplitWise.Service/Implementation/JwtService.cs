using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using SplitWise.Domain;
using SplitWise.Domain.Data;
using SplitWise.Domain.Generic.Entity;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class JwtService(IOptions<JwtSetting> settings) : IJwtService
{
    private readonly JwtSetting _settings = settings.Value;

    public string GenerateAccessToken(User user)
    {
        List<Claim> claims =
        [
            new(SplitWiseConstants.USER_ID, user.Id.ToString()),
            new(SplitWiseConstants.EMAIL,user.Email),
            new(SplitWiseConstants.NAME,user.Username)
        ];

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_settings.Key));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(_settings.AccessTokenExpiryMinutes)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
