using Microsoft.EntityFrameworkCore;
using SplitWise.Domain.Data;
using SplitWise.Repository.Interface;

namespace SplitWise.Repository.Implementation;

public class GroupMemberRepository(ApplicationContext context)  : IGroupMemberRepository
{
    private readonly ApplicationContext _context = context;
    public async Task DeleteMember(Guid id, Guid groupId)
    {
        var groupMember = await _context.GroupMembers
            .FirstOrDefaultAsync(x => x.Groupid == groupId && x.Memberid == id);
        if (groupMember == null)
        {
            throw new Exception("Group member not found");
        }

        groupMember.Isdeleted = true;
        await _context.SaveChangesAsync();
    }
}