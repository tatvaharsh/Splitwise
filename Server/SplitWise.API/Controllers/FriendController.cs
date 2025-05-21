using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Response;
using SplitWise.Service;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/Friend")]
public class FriendController(IFriendService friendService, IAppContextService appContextService, IActivityService activityService,
IGroupMemberService groupMemberService) : BaseController
{
    private readonly IFriendService _friendService = friendService;
    private readonly IActivityService _activityService = activityService;
    private readonly IGroupMemberService _groupMemberService = groupMemberService;
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
        List<FriendResponse> res = await _friendService.GetAllListQuery();
        return SuccessResponse(content: res);
    }

    [HttpGet("getfriend/{id}")]
    public async Task<IActionResult> GetFriendById([FromRoute] Guid id)
    {
        GetFriendresponse getFriendresponse = await _friendService.GetFriendDetails(id);
        return SuccessResponse(content: getFriendresponse);
    }
}