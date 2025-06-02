using SplitWise.Domain;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Repository.Interface;
using SplitWise.Service.Implementation;
using SplitWise.Service.Interface;

public class AuthService(IBaseRepository<User> baseRepository) : BaseService<User>(baseRepository), IAuthService
{
    public async Task<string> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await GetOneAsync(x=>x.Email == request.Email);

        if (existingUser != null)
            return "Email already exists.";

        User user = new()
        {
            Username = request.Username,
            Email = request.Email,
            Phone = request.Phone,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        await AddAsync(user);
        return SplitWiseConstants.REGISTER;
    }
}