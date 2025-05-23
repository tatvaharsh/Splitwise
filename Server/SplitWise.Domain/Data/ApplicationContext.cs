using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SplitWise.Domain.Data;

public partial class ApplicationContext : DbContext
{
    public ApplicationContext()
    {
    }

    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Activity> Activities { get; set; }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<ActivitySplit> ActivitySplits { get; set; }

    public virtual DbSet<ExpenseLogo> ExpenseLogos { get; set; }

    public virtual DbSet<FriendCollection> FriendCollections { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupMember> GroupMembers { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<UpiPayment> UpiPayments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=SplitWise;Username=postgres;Password=A1@laremine;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activities_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Isdeleted).HasDefaultValue(false);
            entity.Property(e => e.Time).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UserInvolvement).HasDefaultValue(true);

            entity.HasOne(d => d.ExpenseLogoNavigation).WithMany(p => p.Activities)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("activities_expense_logo_fkey");

            entity.HasOne(d => d.Group).WithMany(p => p.Activities)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("activities_groupid_fkey");

            entity.HasOne(d => d.Paidby).WithMany(p => p.Activities)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("activities_paidbyid_fkey");
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activity_log_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("activity_log_user_id_fkey");
        });

        modelBuilder.Entity<ActivitySplit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activity_splits_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Isdeleted).HasDefaultValue(false);

            entity.HasOne(d => d.Activity).WithMany(p => p.ActivitySplits)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("activity_splits_activityid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ActivitySplits)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("activity_splits_userid_fkey");
        });

        modelBuilder.Entity<ExpenseLogo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("expense_logos_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Isdeleted).HasDefaultValue(false);
        });

        modelBuilder.Entity<FriendCollection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("friend_collections_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Isdeleted).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValueSql("'pending'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Friend).WithMany(p => p.FriendCollectionFriends)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("friend_collections_friendid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.FriendCollectionUsers)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("friend_collections_userid_fkey");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("groups_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Isdeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Creator).WithMany(p => p.Groups)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("groups_creatorid_fkey");
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_members_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Isdeleted).HasDefaultValue(false);
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupMembers)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("group_members_groupid_fkey");

            entity.HasOne(d => d.Member).WithMany(p => p.GroupMembers)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("group_members_memberid_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Isdeleted).HasDefaultValue(false);
            entity.Property(e => e.Read).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("notifications_userid_fkey");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transactions_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Isdeleted).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValueSql("'pending'::character varying");
            entity.Property(e => e.Time).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Activity).WithMany(p => p.Transactions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("transactions_activityid_fkey");

            entity.HasOne(d => d.Group).WithMany(p => p.Transactions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("transactions_groupid_fkey");

            entity.HasOne(d => d.Payer).WithMany(p => p.TransactionPayers)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("transactions_payerid_fkey");

            entity.HasOne(d => d.Receiver).WithMany(p => p.TransactionReceivers)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("transactions_receiverid_fkey");
        });

        modelBuilder.Entity<UpiPayment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("upi_payments_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Isdeleted).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValueSql("'initiated'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Transaction).WithMany(p => p.UpiPayments)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("upi_payments_transactionid_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Isdeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        OnModelCreatingPartial(modelBuilder);
        OnModelCreatingPartialCustom(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    partial void OnModelCreatingPartialCustom(ModelBuilder modelBuilder);
}
