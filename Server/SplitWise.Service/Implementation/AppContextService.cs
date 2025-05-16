using Microsoft.AspNetCore.Http;
using SplitWise.Domain;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class AppContextService(IHttpContextAccessor httpContextAccessor) : IAppContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    public string GetBaseURL()
    {
        HttpContext? context = _httpContextAccessor.HttpContext;
        HttpRequest? request = context?.Request;
        return request != null ? $"{request.Scheme}://{request.Host}" : string.Empty;
    }

    public Guid? GetUserId()
    {
        string userIdClaim = _httpContextAccessor.HttpContext?.User?.Claims
            ?.FirstOrDefault(x => x.Type == SplitWiseConstants.USER_ID)?.Value;

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }
}
