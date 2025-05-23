using SplitWise.Domain.Data;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class UserService(IBaseRepository<User> baseRepository) : BaseService<User>(baseRepository), IUserService
{
}
