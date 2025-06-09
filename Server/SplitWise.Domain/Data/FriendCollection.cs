using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SplitWise.Domain.Data;

[Table("friend_collections")]
public partial class FriendCollection
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("userid")]
    public Guid? Userid { get; set; }

    [Column("friendid")]
    public Guid? Friendid { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isdeleted")]
    public bool Isdeleted { get; set; }

    [ForeignKey("Friendid")]
    [InverseProperty("FriendCollectionFriends")]
    public virtual User? Friend { get; set; }

    [ForeignKey("Userid")]
    [InverseProperty("FriendCollectionUsers")]
    public virtual User? User { get; set; }
}
