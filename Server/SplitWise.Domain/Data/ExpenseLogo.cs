using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SplitWise.Domain.Data;

[Table("expense_logos")]
public partial class ExpenseLogo
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("logo_name")]
    [StringLength(100)]
    public string LogoName { get; set; } = null!;

    [Column("isdeleted")]
    public bool Isdeleted { get; set; }

    [InverseProperty("ExpenseLogoNavigation")]
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
