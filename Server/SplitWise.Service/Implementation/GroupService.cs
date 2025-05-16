using SplitWise.Domain;
using SplitWise.Domain.Data;
using SplitWise.Domain.DTO.Requests;
using SplitWise.Domain.Generic.Helper;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class GroupService(IBaseRepository<Group> baseRepository,  IAppContextService appContextService, IActivityService activityService,
IGroupMemberService groupMemberService) : BaseService<Group>(baseRepository), IGroupService
{
    private readonly IAppContextService _appContextService = appContextService;
    private readonly IActivityService _activityService = activityService;
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
        if(request.AutoLogo != null){
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

        Activity activityEntity = new()
        {
            Description = $"You created the group '{groupEntity.Groupname}'",
            Groupid = groupEntity.Id,  
            UserInvolvement = false, 
            CreatedAt = DateTime.UtcNow,
        };
        await _activityService.AddAsync(activityEntity);
        return SplitWiseConstants.RECORD_CREATED;
    }

    public async Task<string> UpdateGroupAsync(GroupUpdateRequest request)
    {
        Group groupEntity = await GetOneAsync(x=>x.Id == request.Id) ?? throw new NotFoundException();

        groupEntity.Groupname = request.GroupName;
        if (request.AutoLogo != null)
        {
            string fileUrl = await FileHelper.UploadFile(request.AutoLogo, SplitWiseConstants.CUSTOMER_FOLDER);
            groupEntity.AutoLogo = fileUrl;
        }
        groupEntity.UpdatedAt = DateTime.UtcNow;
        await UpdateAsync(groupEntity);
        // Activity activityEntity = new()
        // {
        //     Description = $"You updated the group '{groupEntity.Groupname}'",
        //     Groupid = groupEntity.Id,  
        //     UserInvolvement = false, 
        //     CreatedAt = DateTime.UtcNow,
        // };
        // await _activityService.AddAsync(activityEntity);
        return SplitWiseConstants.RECORD_UPDATED;
    }

}
