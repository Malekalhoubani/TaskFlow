using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Entities.Authentication;
using TaskFlow.Entities.Users;
using TaskFlow.Entities.Security;

namespace TaskFlow.Repo.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginActivity> LoginActivities => Set<LoginActivity>();

    // Entity Framework Core Fluent API Configuration
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(token => token.Id);

            entity.Property(token => token.TokenHash)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasIndex(token => token.TokenHash)
                .IsUnique();

            entity.Property(token => token.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasIndex(token => token.UserId);

            entity.HasIndex(token => new
            {
                token.UserId,
                token.FamilyId
            });

            entity.Property(token => token.ReplacedByTokenHash)
                .HasMaxLength(128);

            entity.Property(token => token.CreatedByIp)
                .HasMaxLength(45);

            entity.Property(token => token.RevokedByIp)
                .HasMaxLength(45);

            entity.Property(token => token.UserAgent)
                .HasMaxLength(512);

            entity.Property(token => token.RevocationReason)
                .HasMaxLength(256);

            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LoginActivity>(entity =>
        {
            entity.ToTable("LoginActivities");
            entity.HasKey(activity => activity.Id);
            entity.Property(activity => activity.Email).IsRequired().HasMaxLength(256);
            entity.Property(activity => activity.UserId).HasMaxLength(450);
            entity.Property(activity => activity.FailureReason).HasMaxLength(256);
            entity.Property(activity => activity.IpAddress).HasMaxLength(45);
            entity.Property(activity => activity.UserAgent).HasMaxLength(512);
            entity.HasIndex(activity => new { activity.UserId, activity.CreatedAtUtc });
            entity.HasOne(activity => activity.User)
                .WithMany(user => user.LoginActivities)
                .HasForeignKey(activity => activity.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
