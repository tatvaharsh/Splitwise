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
public class GroupController(IGroupService service, IMapper mapper, IAppContextService appContextService) : BaseController
{
    private readonly IGroupService _service = service;
    private readonly IMapper _mapper = mapper;
    private readonly IAppContextService _appContextService = appContextService;

    [HttpGet("GetList")]
    public async Task<IActionResult> GetList()
    {
        string baseURL = _appContextService.GetBaseURL();
        List<Group> groupEntities = await _service.GetListAsync(x => true, x => x.GroupMembers);
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
    
    [HttpPut("update")]
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
        return SuccessResponse<object>(message: await _service.DeleteAsync(id));
    }

    [HttpGet("get/{id}")]
    public async Task<IActionResult> GetGroupById([FromRoute] Guid id)
    {
        string baseURL = _appContextService.GetBaseURL();
        Group groupEntity = await _service.GetOneAsync(x => x.Id == id, query => query
        .Include(g => g.GroupMembers)
        .ThenInclude(gm => gm.Member) 
        ) ?? throw new Exception();;
        GroupResponse groupResponse = new()
        {
                Id = groupEntity.Id,
                Groupname = groupEntity.Groupname,
                AutoLogo = $"{baseURL}{groupEntity.AutoLogo}",
                TotalMember = groupEntity.GroupMembers.Count,
                Members = groupEntity.GroupMembers.Select(member => new MemberResponse
                {
                    Id =member.Id,
                    Name = member.Member.Username,
                }).ToList()
            };
        return SuccessResponse(content: groupResponse);
        throw  new Exception();
    }
}
