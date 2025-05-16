using Microsoft.AspNetCore.Http;

namespace SplitWise.Domain.DTO.Requests;

public class GroupRequest
{
    public string GroupName { get; set; } = null!;
    public IFormFile? AutoLogo { get; set; }
}
