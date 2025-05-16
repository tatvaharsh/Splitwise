using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SplitWise.Domain.Data;

[Table("transactions")]
public partial class Transaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("payerid")]
    public Guid? Payerid { get; set; }

    [Column("receiverid")]
    public Guid? Receiverid { get; set; }

    [Column("amount")]
    [Precision(10, 2)]
    public decimal Amount { get; set; }

    [Column("method")]
    [StringLength(50)]
    public string? Method { get; set; }

    [Column("time")]
    public DateTime? Time { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("groupid")]
    public Guid? Groupid { get; set; }

    [Column("activityid")]
    public Guid? Activityid { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isdeleted")]
    public bool Isdeleted { get; set; }

    [ForeignKey("Activityid")]
    [InverseProperty("Transactions")]
    public virtual Activity? Activity { get; set; }

    [ForeignKey("Groupid")]
    [InverseProperty("Transactions")]
    public virtual Group? Group { get; set; }

    [ForeignKey("Payerid")]
    [InverseProperty("TransactionPayers")]
    public virtual User? Payer { get; set; }

    [ForeignKey("Receiverid")]
    [InverseProperty("TransactionReceivers")]
    public virtual User? Receiver { get; set; }

    [InverseProperty("Transaction")]
    public virtual ICollection<UpiPayment> UpiPayments { get; set; } = new List<UpiPayment>();
}
