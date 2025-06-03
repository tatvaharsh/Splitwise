using Microsoft.AspNetCore.Mvc;
using SplitWise.Domain;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/Auth")]
public class AuthController(IAuthService authService, IJwtService jwtService)  : BaseController
{
    private readonly IAuthService _authService = authService;
    private readonly IJwtService _jwtService = jwtService;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { Errors = errors });
        }

        var result = await _authService.RegisterAsync(request);
        return SuccessResponse(content: result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _authService.GetOneAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            throw new CustomException(StatusCodes.Status400BadRequest, SplitWiseConstants.INVALID_LOGIN);
        var token = _jwtService.GenerateAccessToken(user);
        return SuccessResponse(content : token);
    }
}