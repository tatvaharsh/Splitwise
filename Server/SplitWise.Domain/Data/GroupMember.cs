using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SplitWise.Domain.Data;

[Table("group_members")]
public partial class GroupMember
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("groupid")]
    public Guid? Groupid { get; set; }

    [Column("memberid")]
    public Guid? Memberid { get; set; }

    [Column("joined_at")]
    public DateTime? JoinedAt { get; set; }

    [Column("isdeleted")]
    public bool Isdeleted { get; set; }

    [ForeignKey("Groupid")]
    [InverseProperty("GroupMembers")]
    public virtual Group? Group { get; set; }

    [ForeignKey("Memberid")]
    [InverseProperty("GroupMembers")]
    public virtual User? Member { get; set; }
}
