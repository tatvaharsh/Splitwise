using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SplitWise.Domain.Data;

public partial class ApplicationContext
{
    partial void OnModelCreatingPartialCustom(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>().HasQueryFilter(e => !e.Isdeleted);
        modelBuilder.Entity<ActivitySplit>().HasQueryFilter(e => !e.Isdeleted);
        modelBuilder.Entity<ExpenseLogo>().HasQueryFilter(e => !e.Isdeleted);
        modelBuilder.Entity<FriendCollection>().HasQueryFilter(e => !e.Isdeleted);
        modelBuilder.Entity<Group>().HasQueryFilter(e => !e.Isdeleted);
        modelBuilder.Entity<GroupMember>().HasQueryFilter(e => !e.Isdeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.Isdeleted);
        modelBuilder.Entity<Transaction>().HasQueryFilter(e => !e.Isdeleted);
        modelBuilder.Entity<UpiPayment>().HasQueryFilter(e => !e.Isdeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.Isdeleted);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                    ));
                }
            }
        }
    }
}
