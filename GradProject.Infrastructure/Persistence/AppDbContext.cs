using GradProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Profile> Profiles => Set<Profile>();
        public DbSet<RunningActivity> RunningActivities => Set<RunningActivity>();

        public DbSet<Food> Foods => Set<Food>();
        public DbSet<ConsumedFood> ConsumedFoods => Set<ConsumedFood>();
        public DbSet<DailySummary> DailySummaries => Set<DailySummary>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.Id);

                e.Property(u => u.Email)
                 .IsRequired()
                 .HasMaxLength(256);

                e.HasIndex(u => u.Email).IsUnique();

                e.Property(u => u.Role)
                 .HasConversion<int>();

                e.Navigation(u => u.Profile).IsRequired(false);
            });

            modelBuilder.Entity<Profile>(e =>
            {
                e.HasKey(p => p.Id);

                e.Property(p => p.FirstName)
                 .IsRequired()
                 .HasMaxLength(100);

                e.Property(p => p.LastName)
                 .HasMaxLength(100);

                e.Property(p => p.Height)
                 .HasPrecision(5, 2);

                e.Property(p => p.Weight)
                 .HasPrecision(5, 2);

                e.Property(p => p.Gender)
                 .HasConversion<int>();

                e.Property(p => p.ActivityLevel)
                 .HasConversion<int>();

                e.HasOne(p => p.User)
                 .WithOne(u => u.Profile)
                 .HasForeignKey<Profile>(p => p.UserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(p => p.UserId).IsUnique();
            });

            modelBuilder.Entity<RunningActivity>(e =>
            {
                e.HasKey(r => r.Id);

                e.Property(r => r.ExternalActivityId)
                 .IsRequired()
                 .HasMaxLength(100);

                e.Property(r => r.Name)
                 .IsRequired()
                 .HasMaxLength(256);

                e.Property(r => r.Type)
                 .IsRequired()
                 .HasMaxLength(50);

                e.Property(r => r.StartTime)
                 .IsRequired();

                e.Property(r => r.CreatedAt)
                 .IsRequired();

                e.Property(r => r.UpdatedAt)
                 .IsRequired();

                e.Property(r => r.DistanceMeters)
                 .IsRequired();

                e.Property(r => r.MovingTimeSeconds)
                 .IsRequired();

                e.Property(r => r.ElapsedTimeSeconds)
                 .IsRequired();

                e.Property(r => r.TotalElevationGain)
                 .IsRequired();

                e.Property(r => r.AverageSpeed)
                 .IsRequired();


                e.HasIndex(r => new { r.UserId, r.ExternalActivityId })
                 .IsUnique()
                 .HasDatabaseName("IX_RunningActivities_UserId_ExternalActivityId");

                e.HasIndex(r => r.UserId)
                 .HasDatabaseName("IX_RunningActivities_UserId");

                e.HasIndex(r => r.StartTime)
                 .HasDatabaseName("IX_RunningActivities_StartTime");

                e.HasOne(r => r.User)
                 .WithMany()
                 .HasForeignKey(r => r.UserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_RunningActivities_DistanceMeters_NonNegative", "\"DistanceMeters\" >= 0");
                    t.HasCheckConstraint("CK_RunningActivities_MovingTimeSeconds_NonNegative", "\"MovingTimeSeconds\" >= 0");
                    t.HasCheckConstraint("CK_RunningActivities_ElapsedTimeSeconds_NonNegative", "\"ElapsedTimeSeconds\" >= 0");
                    t.HasCheckConstraint("CK_RunningActivities_TotalElevationGain_NonNegative", "\"TotalElevationGain\" >= 0");
                    t.HasCheckConstraint("CK_RunningActivities_AverageSpeed_NonNegative", "\"AverageSpeed\" >= 0");
                });
            });

            modelBuilder.Entity<Food>(e =>
            {
                e.HasKey(f => f.Id);

                e.Property(f => f.Name)
                 .IsRequired()
                 .HasMaxLength(200);

                e.Property(f => f.Category)
                 .IsRequired()
                 .HasMaxLength(100);

                e.Property(f => f.Source)
                 .IsRequired()
                 .HasMaxLength(50);

                e.Property(f => f.Aliases)
                 .HasColumnType("text[]");

                e.Property(f => f.Kcal).HasPrecision(8, 2);
                e.Property(f => f.ProteinG).HasPrecision(8, 2);
                e.Property(f => f.FatG).HasPrecision(8, 2);
                e.Property(f => f.CarbG).HasPrecision(8, 2);
                e.Property(f => f.SugarG).HasPrecision(8, 2);
                e.Property(f => f.FiberG).HasPrecision(8, 2);
                e.Property(f => f.SodiumMg).HasPrecision(10, 2);
                e.Property(f => f.DefaultPortionG).HasPrecision(8, 2);

                e.HasIndex(f => f.Name)
                 .IsUnique()
                 .HasDatabaseName("IX_Foods_Name");

                e.HasIndex(f => f.Category)
                 .HasDatabaseName("IX_Foods_Category");

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Foods_Kcal_NonNegative", "\"Kcal\" >= 0");
                    t.HasCheckConstraint("CK_Foods_ProteinG_NonNegative", "\"ProteinG\" >= 0");
                    t.HasCheckConstraint("CK_Foods_FatG_NonNegative", "\"FatG\" >= 0");
                    t.HasCheckConstraint("CK_Foods_CarbG_NonNegative", "\"CarbG\" >= 0");
                    t.HasCheckConstraint("CK_Foods_SugarG_NonNegative", "\"SugarG\" >= 0");
                    t.HasCheckConstraint("CK_Foods_FiberG_NonNegative", "\"FiberG\" >= 0");
                    t.HasCheckConstraint("CK_Foods_SodiumMg_NonNegative", "\"SodiumMg\" >= 0");
                    t.HasCheckConstraint("CK_Foods_DefaultPortionG_Positive", "\"DefaultPortionG\" > 0");
                });
            });

            modelBuilder.Entity<ConsumedFood>(e =>
            {
                e.HasKey(cf => cf.Id);

                e.Property(cf => cf.ConsumedAt)
                 .IsRequired();

                e.Property(cf => cf.PortionG)
                 .IsRequired()
                 .HasPrecision(10, 2);

                e.HasIndex(cf => cf.UserId)
                 .HasDatabaseName("IX_ConsumedFoods_UserId");

                e.HasIndex(cf => new { cf.UserId, cf.ConsumedAt })
                 .HasDatabaseName("IX_ConsumedFoods_UserId_ConsumedAt");

                e.HasIndex(cf => cf.FoodId)
                 .HasDatabaseName("IX_ConsumedFoods_FoodId");

                e.HasOne(cf => cf.User)
                 .WithMany()
                 .HasForeignKey(cf => cf.UserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(cf => cf.Food)
                 .WithMany()
                 .HasForeignKey(cf => cf.FoodId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Restrict);

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_ConsumedFoods_PortionG_Positive", "\"PortionG\" > 0");
                });
            });

            modelBuilder.Entity<DailySummary>(e =>
            {
                e.HasKey(ds => ds.Id);

                e.Property(ds => ds.Date)
                 .IsRequired();

                e.Property(ds => ds.TotalProtein).HasPrecision(10, 2);
                e.Property(ds => ds.TotalCarbs).HasPrecision(10, 2);
                e.Property(ds => ds.TotalFat).HasPrecision(10, 2);

                e.HasIndex(ds => new { ds.UserId, ds.Date })
                 .IsUnique()
                 .HasDatabaseName("IX_DailySummaries_UserId_Date");

                e.HasIndex(ds => ds.UserId)
                 .HasDatabaseName("IX_DailySummaries_UserId");

                e.HasIndex(ds => ds.Date)
                 .HasDatabaseName("IX_DailySummaries_Date");

                e.HasOne(ds => ds.User)
                 .WithMany()
                 .HasForeignKey(ds => ds.UserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_DailySummaries_TotalIntakeCalories_NonNegative", "\"TotalIntakeCalories\" >= 0");
                    t.HasCheckConstraint("CK_DailySummaries_TotalProtein_NonNegative", "\"TotalProtein\" >= 0");
                    t.HasCheckConstraint("CK_DailySummaries_TotalCarbs_NonNegative", "\"TotalCarbs\" >= 0");
                    t.HasCheckConstraint("CK_DailySummaries_TotalFat_NonNegative", "\"TotalFat\" >= 0");
                });
            });
        }
    }
}
