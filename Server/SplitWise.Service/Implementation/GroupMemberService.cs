using SplitWise.Domain.Data;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class GroupMemberService(IBaseRepository<GroupMember> baseRepository) : BaseService<GroupMember>(baseRepository), IGroupMemberService
{
}
