using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    // private readonly IAuthService _service = service;

    // [HttpPost("register")]
    // public async Task<IActionResult> Register(RegisterRequest request)
    // {
    //     var result = await _service.RegisterAsync(request);

    //     if (result != "Registration successful.")
    //         return BadRequest(result);

    //     return Ok(result);
    // }
    //  public IActionResult Index()
    // {
    //     // Manually create a claim
    //     var claims = new List<Claim>
    //     {
    //         new Claim("USER_ID", "78c89439-8cb5-4e93-8565-de9b7cf6c6ae"),
    //         new Claim(ClaimTypes.Name, "Test User")
    //     };

    //     // Create an identity and principal
    //     var identity = new ClaimsIdentity(claims, "TestAuthType");
    //     var principal = new ClaimsPrincipal(identity);

    //     // Assign it to HttpContext.User
    //     HttpContext.User = principal;

    //     // Now you can test your code
    //     string userIdClaim = HttpContext.User?.Claims
    //         ?.FirstOrDefault(x => x.Type == "USER_ID")?.Value;

    //     return Content($"UserIdClaim = {userIdClaim}");
    // }
}
