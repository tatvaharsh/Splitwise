using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SplitWise.Domain.Data;

[Table("activity_splits")]
public partial class ActivitySplit
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("activityid")]
    public Guid? Activityid { get; set; }

    [Column("userid")]
    public Guid? Userid { get; set; }

    [Column("splitamount")]
    [Precision(10, 2)]
    public decimal Splitamount { get; set; }

    [Column("isdeleted")]
    public bool Isdeleted { get; set; }

    [ForeignKey("Activityid")]
    [InverseProperty("ActivitySplits")]
    public virtual Activity? Activity { get; set; }

    [ForeignKey("Userid")]
    [InverseProperty("ActivitySplits")]
    public virtual User? User { get; set; }
}
