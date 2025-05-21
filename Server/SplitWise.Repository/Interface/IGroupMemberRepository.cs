
namespace SplitWise.Repository.Interface;

public interface IGroupMemberRepository
{
    Task DeleteMember(Guid id, Guid groupId);

}
