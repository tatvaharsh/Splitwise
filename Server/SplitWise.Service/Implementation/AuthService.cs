// using SplitWise.Domain.Data;
// using SplitWise.Domain.DTO.Requests;
// using SplitWise.Service.Interface;

// public class AuthService : IAuthService
// {
//     private readonly IUserRepository _userRepository;

//     public AuthService(IUserRepository userRepository)
//     {
//         _userRepository = userRepository;
//     }

//     public async Task<string> RegisterAsync(RegisterRequest request)
//     {
//         var existingUser = await _userRepository.GetByEmailAsync(request.Email);

//         if (existingUser != null)
//             return "Email already exists.";

//         var user = new User
//         {
//             Id = Guid.NewGuid(),
//             Username = request.Username,
//             Email = request.Email,
//             Phone = request.Phone,
//             Name = request.Name,
//             Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow
//         };

//         await _userRepository.AddAsync(user);

//         return "Registration successful.";
//     }

//     public async Task<User?> LoginAsync(LoginRequest request)
//     {
//         var user = await _userRepository.GetByEmailAsync(request.Email);

//         if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
//             return null;

//         return user;
//     }
// }
