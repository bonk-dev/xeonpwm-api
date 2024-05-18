using Microsoft.EntityFrameworkCore;
using XeonPwm.Api.Models.Db;

namespace XeonPwm.Api.Contexts;

public class XeonPwmContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<AuthToken> Tokens { get; set; } = null!;
    public DbSet<RegisteredAutoDriverPoint> DriverPoints { get; set; } = null!;
     
    public XeonPwmContext(DbContextOptions<XeonPwmContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthToken>()
            .HasOne<User>(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .IsRequired();
        modelBuilder.Entity<AuthToken>()
            .HasIndex(t => t.Token)
            .IsUnique();
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}