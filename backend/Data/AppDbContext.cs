using HelpDesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<TicketStatusHistory> TicketStatusHistories => Set<TicketStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(x => x.Role)
            .HasMaxLength(32);

        modelBuilder.Entity<Ticket>()
            .Property(x => x.Title)
            .HasMaxLength(160);

        modelBuilder.Entity<Ticket>()
            .Property(x => x.Description)
            .HasMaxLength(4000);

        modelBuilder.Entity<Ticket>()
            .Property(x => x.Category)
            .HasMaxLength(64);

        modelBuilder.Entity<Ticket>()
            .Property(x => x.Priority)
            .HasMaxLength(32);

        modelBuilder.Entity<Ticket>()
            .Property(x => x.Status)
            .HasMaxLength(32);

        modelBuilder.Entity<Ticket>()
            .HasOne(x => x.CreatorUser)
            .WithMany()
            .HasForeignKey(x => x.CreatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(x => x.AssignedAgent)
            .WithMany()
            .HasForeignKey(x => x.AssignedAgentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TicketComment>()
            .Property(x => x.Content)
            .HasMaxLength(4000);

        modelBuilder.Entity<TicketComment>()
            .Property(x => x.Visibility)
            .HasMaxLength(32);

        modelBuilder.Entity<TicketComment>()
            .HasOne(x => x.ParentComment)
            .WithMany()
            .HasForeignKey(x => x.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActivityLog>()
            .Property(x => x.ActionType)
            .HasMaxLength(64);

        modelBuilder.Entity<ActivityLog>()
            .Property(x => x.Description)
            .HasMaxLength(1000);

        modelBuilder.Entity<TicketStatusHistory>()
            .Property(x => x.OldStatus)
            .HasMaxLength(32);

        modelBuilder.Entity<TicketStatusHistory>()
            .Property(x => x.NewStatus)
            .HasMaxLength(32);

        base.OnModelCreating(modelBuilder);
    }
}
