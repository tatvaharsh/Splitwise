using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.DTO.Response;
using SplitWise.Service;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/Friend")]
public class FriendController(IFriendService friendService) : BaseController
{
    private readonly IFriendService _friendService = friendService;

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
        FriendResponse res = await _friendService.GetAllListQuery();
        return SuccessResponse(content: res);
    }

    [HttpGet("getfriend/{id}")]
    public async Task<IActionResult> GetFriendById([FromRoute] Guid id)
    {
        GetFriendresponse getFriendresponse = await _friendService.GetFriendDetails(id);
        return SuccessResponse(content: getFriendresponse);
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddFriend([FromBody] AddFriend friend)
    {
        if (friend == null || string.IsNullOrWhiteSpace(friend.Name))
            return BadRequest("Invalid Friend data");

        return SuccessResponse<object>(message: await _friendService.AddFriendAsync(friend));
    }

    [HttpPost("accept/{friendId}")]
    public async Task<IActionResult> AcceptFriend([FromRoute] Guid friendId)
    {
        if (friendId == Guid.Empty)
            return BadRequest("Invalid Friend ID");

        var result = await _friendService.AcceptFriendAsync(friendId);
        return SuccessResponse<object>(message: result);
    }

    [HttpPost("reject/{friendId}")]
    public async Task<IActionResult> RejectFriend([FromRoute] Guid friendId)
    {
        if (friendId == Guid.Empty)
            return BadRequest("Invalid Friend ID");

        var result = await _friendService.RejectFriendAsync(friendId);
        return SuccessResponse<object>(message: result);
    }
}