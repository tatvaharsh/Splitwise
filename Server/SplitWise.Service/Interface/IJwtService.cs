using SplitWise.Domain.Data;

namespace SplitWise.Service.Interface;

public interface IJwtService
{
    string GenerateAccessToken(User user);
}
