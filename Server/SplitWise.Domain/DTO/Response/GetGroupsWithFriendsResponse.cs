namespace SplitWise.Domain.DTO.Response;

public class GetGroupsWithFriendsResponse
{
    public List<OnlyGroupResponse>? Groups { get; set; }
    public List<MemberResponse>? Friends { get; set; }
}

public class OnlyGroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public List<MemberResponse> Members { get; set; } = null!;
}