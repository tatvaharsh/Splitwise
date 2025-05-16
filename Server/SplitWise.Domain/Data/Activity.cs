using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SplitWise.Domain.Data;

[Table("activities")]
public partial class Activity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("paidbyid")]
    public Guid? Paidbyid { get; set; }

    [Column("groupid")]
    public Guid? Groupid { get; set; }

    [Column("time")]
    public DateTime? Time { get; set; }

    [Column("amount")]
    [Precision(10, 2)]
    public decimal? Amount { get; set; }

    [Column("user_involvement")]
    public bool? UserInvolvement { get; set; }

    [Column("expense_logo")]
    public Guid? ExpenseLogo { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isdeleted")]
    public bool Isdeleted { get; set; }

    [InverseProperty("Activity")]
    public virtual ICollection<ActivitySplit> ActivitySplits { get; set; } = new List<ActivitySplit>();

    [ForeignKey("ExpenseLogo")]
    [InverseProperty("Activities")]
    public virtual ExpenseLogo? ExpenseLogoNavigation { get; set; }

    [ForeignKey("Groupid")]
    [InverseProperty("Activities")]
    public virtual Group? Group { get; set; }

    [ForeignKey("Paidbyid")]
    [InverseProperty("Activities")]
    public virtual User? Paidby { get; set; }

    [InverseProperty("Activity")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
