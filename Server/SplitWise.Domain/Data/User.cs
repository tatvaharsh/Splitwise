using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SplitWise.Domain.Data;

[Table("users")]
[Index("Email", Name = "users_email_key", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("username")]
    [StringLength(100)]
    public string Username { get; set; } = null!;

    [Column("email")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Column("phone")]
    [StringLength(15)]
    public string? Phone { get; set; }

    [Column("password")]
    [StringLength(255)]
    public string Password { get; set; } = null!;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isdeleted")]
    public bool Isdeleted { get; set; }

    [InverseProperty("Paidby")]
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    [InverseProperty("User")]
    public virtual ICollection<ActivitySplit> ActivitySplits { get; set; } = new List<ActivitySplit>();

    [InverseProperty("Friend")]
    public virtual ICollection<FriendCollection> FriendCollectionFriends { get; set; } = new List<FriendCollection>();

    [InverseProperty("User")]
    public virtual ICollection<FriendCollection> FriendCollectionUsers { get; set; } = new List<FriendCollection>();

    [InverseProperty("Member")]
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    [InverseProperty("Creator")]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("Payer")]
    public virtual ICollection<Transaction> TransactionPayers { get; set; } = new List<Transaction>();

    [InverseProperty("Receiver")]
    public virtual ICollection<Transaction> TransactionReceivers { get; set; } = new List<Transaction>();
}
