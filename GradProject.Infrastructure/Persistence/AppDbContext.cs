using GradProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Profile> Profiles => Set<Profile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // USER
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.Id);

                e.Property(u => u.Email)
                 .IsRequired()
                 .HasMaxLength(256);

                e.HasIndex(u => u.Email).IsUnique();

                e.Property(u => u.Role)
                 .HasConversion<int>();
            });

            // PROFILE
            modelBuilder.Entity<Profile>(e =>
            {
                e.HasKey(p => p.Id);

                e.Property(p => p.FirstName)
                 .IsRequired()
                 .HasMaxLength(100);

                e.Property(p => p.LastName)
                 .HasMaxLength(100);

                // Decimal precision (PostgreSQL → numeric(5,2))
                e.Property(p => p.Height)
                 .HasPrecision(5, 2);

                e.Property(p => p.Weight)
                 .HasPrecision(5, 2);

                e.Property(p => p.Gender)
                 .HasConversion<int>();

                // 1-1 ilişki
                e.HasOne(p => p.User)
                 .WithOne(u => u.Profile)
                 .HasForeignKey<Profile>(p => p.UserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);

                // Her user için tek profile
                e.HasIndex(p => p.UserId).IsUnique();
            });
        }
    }
}
