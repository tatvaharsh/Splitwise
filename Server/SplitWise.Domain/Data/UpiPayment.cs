using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SplitWise.Domain.Data;

[Table("upi_payments")]
public partial class UpiPayment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("transactionid")]
    public Guid? Transactionid { get; set; }

    [Column("upiid")]
    [StringLength(100)]
    public string Upiid { get; set; } = null!;

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isdeleted")]
    public bool Isdeleted { get; set; }

    [ForeignKey("Transactionid")]
    [InverseProperty("UpiPayments")]
    public virtual Transaction? Transaction { get; set; }
}
