namespace SplitWise.Domain.DTO.Response;

public class GroupResponse
{
    public Guid Id { get; set; }
    public string Groupname { get; set; } = null!;
    public string? AutoLogo { get; set; }
    public int TotalMember {get; set;}
    public List<MemberResponse>? Members { get; set; } = new();
}

public class MemberResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}
