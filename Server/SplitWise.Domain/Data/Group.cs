using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SplitWise.Domain.Data;

[Table("groups")]
public partial class Group
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("creatorid")]
    public Guid? Creatorid { get; set; }

    [Column("groupname")]
    [StringLength(100)]
    public string Groupname { get; set; } = null!;

    [Column("auto_logo")]
    [StringLength(255)]
    public string? AutoLogo { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isdeleted")]
    public bool Isdeleted { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    [ForeignKey("Creatorid")]
    [InverseProperty("Groups")]
    public virtual User? Creator { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    [InverseProperty("Group")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
