using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;

namespace SplitWise.Service.Interface;

public interface IAuthService: IBaseService<User>
{
    Task<string> RegisterAsync(RegisterRequest request);
}
