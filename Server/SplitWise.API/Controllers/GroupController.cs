using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.DTO.Response;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/Group")]
public class GroupController(IGroupService service, IMapper mapper, IAppContextService appContextService, IActivityLoggerService activityLoggerService) : BaseController
{
    private readonly IGroupService _service = service;
    private readonly IActivityLoggerService _activityLoggerService = activityLoggerService;
    private readonly IMapper _mapper = mapper;
    private readonly IAppContextService _appContextService = appContextService;

    [HttpGet("GetList")]
    public async Task<IActionResult> GetList()
    {
        Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        string baseURL = _appContextService.GetBaseURL();
        List<Group> groupEntities = await _service.GetListAsync(
            group => group.GroupMembers.Any(member => member.Memberid == userId),
            query => query.Include(group => group.GroupMembers)
        );
        List<GroupResponse> groupResponses = groupEntities.Select(group => new GroupResponse
        {
            Id = group.Id,
            Groupname = group.Groupname,
            AutoLogo = $"{baseURL}{group.AutoLogo}",
            TotalMember = group.GroupMembers.Count
        }).ToList();
        return SuccessResponse(content: groupResponses);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateGroup([FromForm]GroupRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.GroupName))
            return BadRequest("Invalid Group data");

        return SuccessResponse<object>(message: await _service.CreateGroupAsync(request));
    }
    
    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateGroup([FromRoute] Guid id,[FromForm] GroupUpdateRequest request)
    {
        request.Id = id;
        if (request == null || string.IsNullOrWhiteSpace(request.GroupName))
            return BadRequest("Invalid Group data");

        return SuccessResponse<object>(message: await _service.UpdateGroupAsync(request));
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteGroup([FromRoute] Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest("Invalid Group ID");
            
        var groupMembers = await _service.GetOneAsync(gm => gm.Id == id, query => query
            .Include(g => g.GroupMembers)
            .ThenInclude(m => m.Member)
        ) ?? throw new Exception();

        foreach (var member in groupMembers.GroupMembers)
        {
            if (member.Memberid.HasValue)
            {
                await _activityLoggerService.LogAsync(
                    member.Memberid.Value,
                    $"The group '{member.Group.Groupname}' has been deleted."
                );
            }
        }
        return SuccessResponse<object>(message: await _service.DeleteAsync(id));
    }

    [HttpGet("get/{id}")]
    public async Task<IActionResult> GetGroupById([FromRoute] Guid id)
    {
        string baseURL = _appContextService.GetBaseURL();
        Group groupEntity = await _service.GetOneAsync(x => x.Id == id, query => query
        .Include(g => g.GroupMembers)
        .ThenInclude(m => m.Member)
        ) ?? throw new Exception();
        GroupResponse groupResponse = new()
        {
                Id = groupEntity.Id,
                Groupname = groupEntity.Groupname,
                AutoLogo = $"{baseURL}{groupEntity.AutoLogo}",
                TotalMember = groupEntity.GroupMembers.Count,
                Members = groupEntity.GroupMembers.Select(member => new MemberResponse
                {
                    Id =member.Member.Id,
                    Name = member.Member.Username,
                }).ToList()
            };
        return SuccessResponse(content: groupResponse);
        throw  new Exception();
    }
}
