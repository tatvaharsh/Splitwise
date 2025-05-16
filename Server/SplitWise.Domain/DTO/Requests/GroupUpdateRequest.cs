using Microsoft.AspNetCore.Http;

namespace SplitWise.Domain.DTO.Requests;

public class GroupUpdateRequest
{   
    public Guid Id { get; set; }
    public string GroupName { get; set; } = null!;
    public IFormFile? AutoLogo { get; set; }
}
