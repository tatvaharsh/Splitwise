using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using SplitWise.Domain;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.DTO.Response;
using SplitWise.Domain.Generic.Helper;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class GroupService(IBaseRepository<Group> baseRepository, IAppContextService appContextService,
IGroupMemberService groupMemberService, ApplicationContext applicationContext, IActivityLoggerService activityLoggerService) : BaseService<Group>(baseRepository), IGroupService
{
    private readonly IAppContextService _appContextService = appContextService;
    private readonly IActivityLoggerService _activityLoggerService = activityLoggerService;
    private readonly ApplicationContext _applicationContext = applicationContext;
    private readonly IGroupMemberService _groupMemberService = groupMemberService;

    public async Task<string> CreateGroupAsync(GroupRequest request)
    {
        // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);
        Group groupEntity = new()
        {
            Creatorid = userId,
            Groupname = request.GroupName,
            CreatedAt = DateTime.UtcNow,
        };
        if (request.AutoLogo != null)
        {
            string fileUrl = await FileHelper.UploadFile(request.AutoLogo, SplitWiseConstants.CUSTOMER_FOLDER);
            groupEntity.AutoLogo = fileUrl;
        }
        await AddAsync(groupEntity);

        GroupMember groupMember = new()
        {
            Groupid = groupEntity.Id,
            Memberid = userId,
            JoinedAt = DateTime.UtcNow
        };
        await _groupMemberService.AddAsync(groupMember);

        await _activityLoggerService.LogAsync(userId, $"You created the group '{groupEntity.Groupname}'");
        return SplitWiseConstants.RECORD_CREATED;
    }

    public async Task<List<OnlyGroupResponse>> GetGroupsAsync()
    {
        // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);
        List<Group> groupEntities = await GetListAsync(
                    x => x.GroupMembers.Any(m => m.Memberid == userId),
                    query => query.Include(x => x.GroupMembers)
                    .ThenInclude(m => m.Member));
        List<OnlyGroupResponse> groupResponses = groupEntities.Select(group => new OnlyGroupResponse
        {
            Id = group.Id,
            Name = group.Groupname,
            Members = group.GroupMembers.Select(member => new MemberResponse
            {
                Id = member.Memberid ?? new Guid(),
                Name = member.Memberid == userId ? "You" : member.Member.Username
            }).ToList(),
        }).ToList();
        return groupResponses;
    }


    public async Task<string> UpdateGroupAsync(GroupUpdateRequest request)
    {
        // Guid userId = _appContextService.GetUserId() ?? throw new UnauthorizedAccessException();
        var userIdString = "78c89439-8cb5-4e93-8565-de9b7cf6c6ae";
        Guid userId = Guid.Parse(userIdString);
        Group groupEntity = await GetOneAsync(x => x.Id == request.Id) ?? throw new NotFoundException();

        groupEntity.Groupname = request.GroupName;
        if (request.AutoLogo != null)
        {
            string fileUrl = await FileHelper.UploadFile(request.AutoLogo, SplitWiseConstants.CUSTOMER_FOLDER);
            groupEntity.AutoLogo = fileUrl;
        }
        groupEntity.UpdatedAt = DateTime.UtcNow;
        await UpdateAsync(groupEntity);
        await _activityLoggerService.LogAsync(userId, $"You updated the group '{groupEntity.Groupname}'");

        // Get all members of the group (including the updater)
        var groupMembers = await GetOneAsync(
            gm => gm.Id == request.Id,
            query => query.Include(x => x.GroupMembers) // optional if you want user details
        );

        // Log for all members about the update
        foreach (var member in groupMembers.GroupMembers)
        {
            if (member.Memberid.HasValue)
            {
                if (member.Memberid.Value != userId)
                {
                    await _activityLoggerService.LogAsync(
                        member.Memberid.Value,
                        $"The group '{groupEntity.Groupname}' has been updated."
                    );
                }
            }
        }
        return SplitWiseConstants.RECORD_UPDATED;
    }

}
