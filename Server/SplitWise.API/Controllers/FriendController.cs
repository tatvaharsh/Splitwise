using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Response;
using SplitWise.Service;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/Friend")]
public class FriendController(IFriendService friendService, IAppContextService appContextService) : BaseController
{
    private readonly IFriendService _friendService = friendService;
    private readonly IAppContextService _appContextService = appContextService;

    [HttpGet("get/{id}")]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        List<MemberResponse> friends = await _friendService.GetFriendsDropdown(id) ?? [];
        return SuccessResponse(content: friends);
    }

    [HttpPost("add/{FriendId}/{GroupId}")]
    public async Task<IActionResult> AddFriendToGroup([FromRoute] Guid FriendId, Guid GroupId)
    {
        return SuccessResponse<object>(message: await _friendService.AddFriendIntoGroup(FriendId, GroupId));
    }

    [HttpDelete("delete/{id}/{GroupId}")]
    public async Task<IActionResult> DeleteMember([FromRoute] Guid id, Guid GroupId)
    {
        if (id == Guid.Empty)
            return BadRequest("Invalid Member ID");
        return SuccessResponse<object>(message: await _friendService.DeleteMemberFromGroup(id, GroupId));
    }

    [HttpGet("check-outstanding")]
    public async Task<IActionResult> CheckOutstanding(Guid memberId, Guid groupId)
    {
        var hasOutstanding = await _friendService.CheckOutstanding(memberId, groupId);
        return Ok(hasOutstanding);
    }
    [HttpGet("GetList")]
    public async Task<IActionResult> GetList()
    {
        List<FriendCollection> groupEntities = await _friendService.GetListAsync(x => true,
                    query => query
                            .Include(x => x.User));
        List<GroupResponse> groupResponses = groupEntities.Select(group => new GroupResponse
        {
            Id = group.Id,
            Groupname = group.Groupname,
            AutoLogo = $"{baseURL}{group.AutoLogo}",
            TotalMember = group.GroupMembers.Count
        }).ToList();
        return SuccessResponse(content: groupResponses);
    }


}