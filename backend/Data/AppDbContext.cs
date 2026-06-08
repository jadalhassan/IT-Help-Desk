using HelpDesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

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

        base.OnModelCreating(modelBuilder);
    }
}
