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
        public DbSet<Meal> Meals => Set<Meal>();
        public DbSet<MealFood> MealFoods => Set<MealFood>();
        public DbSet<DailySummary> DailySummaries => Set<DailySummary>();

        public DbSet<Challenge> Challenges => Set<Challenge>();
        public DbSet<Badge> Badges => Set<Badge>();
        public DbSet<UserBadge> UserBadges => Set<UserBadge>();
        public DbSet<UserChallenge> UserChallenges => Set<UserChallenge>();
        public DbSet<LeaderboardSnapshot> LeaderboardSnapshots => Set<LeaderboardSnapshot>();
        public DbSet<FoodAlias> FoodAliases => Set<FoodAlias>();
        public DbSet<Territory> Territories => Set<Territory>();
        public DbSet<TerritoryCell> TerritoryCells => Set<TerritoryCell>();
        public DbSet<UserTerritory> UserTerritories => Set<UserTerritory>();
        public DbSet<TerritoryUnlockCondition> TerritoryUnlockConditions => Set<TerritoryUnlockCondition>();
        public DbSet<TerritoryOwnershipHistory> TerritoryOwnershipHistories => Set<TerritoryOwnershipHistory>();
        public DbSet<AchievementEvent> AchievementEvents => Set<AchievementEvent>();

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

                e.Property(r => r.RunDate)
                 .IsRequired();

                e.Property(r => r.Source)
                 .IsRequired()
                 .HasMaxLength(50);

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

                e.Property(r => r.Route)
                 .HasColumnType("geometry(LineString, 4326)");

                e.HasIndex(r => new { r.UserId, r.ExternalActivityId })
                 .IsUnique()
                 .HasDatabaseName("IX_RunningActivities_UserId_ExternalActivityId");

                e.HasIndex(r => r.UserId)
                 .HasDatabaseName("IX_RunningActivities_UserId");

                e.HasIndex(r => r.StartTime)
                 .HasDatabaseName("IX_RunningActivities_StartTime");

                e.HasIndex(r => r.RunDate)
                 .HasDatabaseName("IX_RunningActivities_RunDate");

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

            modelBuilder.Entity<FoodAlias>(e =>
            {
                e.HasKey(a => a.Id);

                e.Property(a => a.Language)
                 .IsRequired()
                 .HasMaxLength(5);

                e.Property(a => a.Alias)
                 .IsRequired()
                 .HasMaxLength(200);

                e.Property(a => a.NormalizedAlias)
                 .IsRequired()
                 .HasMaxLength(200);

                e.HasOne(a => a.Food)
                 .WithMany()
                 .HasForeignKey(a => a.FoodId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(a => new { a.Language, a.NormalizedAlias })
                 .IsUnique()
                 .HasDatabaseName("IX_FoodAliases_Language_NormalizedAlias");

                e.HasIndex(a => new { a.FoodId, a.Language })
                 .HasDatabaseName("IX_FoodAliases_FoodId_Language");
            });

            modelBuilder.Entity<ConsumedFood>(e =>
            {
                e.HasKey(cf => cf.Id);

                e.Property(cf => cf.ConsumedAt)
                 .IsRequired();

                e.Property(cf => cf.PortionG)
                 .IsRequired()
                 .HasPrecision(10, 2);

                //  NEW: MealId nullable
                e.Property(cf => cf.MealId)
                 .IsRequired(false);

                e.HasIndex(cf => cf.UserId)
                 .HasDatabaseName("IX_ConsumedFoods_UserId");

                e.HasIndex(cf => new { cf.UserId, cf.ConsumedAt })
                 .HasDatabaseName("IX_ConsumedFoods_UserId_ConsumedAt");

                e.HasIndex(cf => cf.FoodId)
                 .HasDatabaseName("IX_ConsumedFoods_FoodId");

                //  NEW: index for MealId
                e.HasIndex(cf => cf.MealId)
                 .HasDatabaseName("IX_ConsumedFoods_MealId");

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

                //  NEW: optional FK ConsumedFood -> Meal (NO SURPRISE CASCADE)
                e.HasOne(cf => cf.Meal)
                 .WithMany() // Meal taraf�nda collection eklemedik (clean + minimal)
                 .HasForeignKey(cf => cf.MealId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Restrict);

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_ConsumedFoods_PortionG_Positive", "\"PortionG\" > 0");
                });
            });

            modelBuilder.Entity<DailySummary>(e =>
            {
                e.HasKey(ds => ds.Id);
                e.Property(ds => ds.Date).IsRequired();

                e.Property(ds => ds.TotalProtein).HasPrecision(10, 2);
                e.Property(ds => ds.TotalCarbs).HasPrecision(10, 2);
                e.Property(ds => ds.TotalFat).HasPrecision(10, 2);

                // Hedef kolonlarý — nullable, ilk aggregation'da bir kez yazýlýr
                e.Property(ds => ds.CalorieTarget).IsRequired(false);
                e.Property(ds => ds.ProteinTargetG).HasPrecision(10, 2).IsRequired(false);
                e.Property(ds => ds.CarbTargetG).HasPrecision(10, 2).IsRequired(false);
                e.Property(ds => ds.FatTargetG).HasPrecision(10, 2).IsRequired(false);

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

            // MEAL
            modelBuilder.Entity<Meal>(e =>
            {
                e.HasKey(m => m.Id);

                e.Property(m => m.MealType)
                 .HasConversion<int>();

                e.Property(m => m.RawText)
                 .HasMaxLength(1000);

                e.Property(m => m.Notes)
                 .HasMaxLength(500);

                e.Property(m => m.LoggedAt)
                 .IsRequired();

                e.Property(m => m.CreatedAt)
                 .IsRequired();

                e.HasIndex(m => m.UserId)
                 .HasDatabaseName("IX_Meals_UserId");

                e.HasIndex(m => m.LoggedAt)
                 .HasDatabaseName("IX_Meals_LoggedAt");

                e.HasIndex(m => new { m.UserId, m.LoggedAt })
                 .HasDatabaseName("IX_Meals_UserId_LoggedAt");

                e.HasOne(m => m.User)
                 .WithMany()
                 .HasForeignKey(m => m.UserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // MEAL FOOD
            modelBuilder.Entity<MealFood>(e =>
            {
                e.HasKey(mf => mf.Id);

                e.Property(mf => mf.Quantity)
                 .IsRequired()
                 .HasPrecision(10, 2);

                e.Property(mf => mf.Unit)
                 .IsRequired()
                 .HasMaxLength(50);

                e.HasIndex(mf => mf.MealId)
                 .HasDatabaseName("IX_MealFoods_MealId");

                e.HasIndex(mf => mf.FoodId)
                 .HasDatabaseName("IX_MealFoods_FoodId");

                e.HasOne(mf => mf.Meal)
                 .WithMany(m => m.MealFoods)
                 .HasForeignKey(mf => mf.MealId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(mf => mf.Food)
                 .WithMany()
                 .HasForeignKey(mf => mf.FoodId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Restrict);

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_MealFoods_Quantity_Positive", "\"Quantity\" > 0");
                });
            });

            // CHALLENGE
            modelBuilder.Entity<Challenge>(e =>
            {
                e.HasKey(c => c.Id);

                e.Property(c => c.Title)
                 .IsRequired()
                 .HasMaxLength(200);

                e.Property(c => c.Description)
                 .HasMaxLength(1000);

                e.Property(c => c.Type)
                 .HasConversion<int>();

                e.Property(c => c.Metric)
                 .HasConversion<int>();

                e.Property(c => c.TargetValue)
                 .IsRequired();

                e.Property(c => c.StartDate)
                 .IsRequired();

                e.Property(c => c.EndDate)
                 .IsRequired();

                e.Property(c => c.RewardPoints)
                 .IsRequired();

                e.Property(c => c.IsCustom)
                 .IsRequired()
                 .HasDefaultValue(false);

                e.Property(c => c.CreatedAtUtc)
                 .IsRequired();

                e.HasIndex(c => c.IsActive)
                 .HasDatabaseName("IX_Challenges_IsActive");

                e.HasIndex(c => c.StartDate)
                 .HasDatabaseName("IX_Challenges_StartDate");

                e.HasIndex(c => c.EndDate)
                 .HasDatabaseName("IX_Challenges_EndDate");

                e.HasIndex(c => c.CreatedByUserId)
                 .HasDatabaseName("IX_Challenges_CreatedByUserId");

                e.HasIndex(c => new { c.IsCustom, c.IsActive })
                 .HasDatabaseName("IX_Challenges_IsCustom_IsActive");

                e.HasOne(c => c.CreatedByUser)
                 .WithMany()
                 .HasForeignKey(c => c.CreatedByUserId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Challenges_TargetValue_NonNegative", "\"TargetValue\" >= 0");
                    t.HasCheckConstraint("CK_Challenges_RewardPoints_NonNegative", "\"RewardPoints\" >= 0");
                    t.HasCheckConstraint("CK_Challenges_EndDate_GreaterThanOrEqual_StartDate", "\"EndDate\" >= \"StartDate\"");
                });
            });

            // BADGE
            modelBuilder.Entity<Badge>(e =>
            {
                e.HasKey(b => b.Id);

                e.Property(b => b.Name)
                 .IsRequired()
                 .HasMaxLength(200);

                e.Property(b => b.Description)
                 .HasMaxLength(1000);

                e.Property(b => b.Type)
                 .HasConversion<int>();

                e.Property(b => b.IconUrl)
                 .HasMaxLength(500);

                e.Property(b => b.PointsReward)
                 .IsRequired();

                e.HasIndex(b => b.IsActive)
                 .HasDatabaseName("IX_Badges_IsActive");

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Badges_PointsReward_NonNegative", "\"PointsReward\" >= 0");
                });
            });

            // USER BADGE (earned badges; one per user-badge pair for non-repeatable badges)
            modelBuilder.Entity<UserBadge>(e =>
            {
                e.HasKey(ub => ub.Id);

                e.Property(ub => ub.EarnedAtUtc)
                 .IsRequired();

                e.HasIndex(ub => ub.UserId)
                 .HasDatabaseName("IX_UserBadges_UserId");

                e.HasIndex(ub => ub.BadgeId)
                 .HasDatabaseName("IX_UserBadges_BadgeId");

                e.HasIndex(ub => ub.EarnedAtUtc)
                 .HasDatabaseName("IX_UserBadges_EarnedAtUtc");

                e.HasIndex(ub => new { ub.UserId, ub.BadgeId })
                 .IsUnique()
                 .HasDatabaseName("UX_UserBadges_UserId_BadgeId");

                e.HasOne(ub => ub.User)
                 .WithMany()
                 .HasForeignKey(ub => ub.UserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ub => ub.Badge)
                 .WithMany()
                 .HasForeignKey(ub => ub.BadgeId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // USER CHALLENGE
            modelBuilder.Entity<UserChallenge>(e =>
            {
                e.HasKey(uc => uc.Id);

                e.Property(uc => uc.JoinedAt)
                 .IsRequired();

                e.Property(uc => uc.ProgressDistanceMeters)
                 .IsRequired()
                 .HasDefaultValue(0L);

                e.Property(uc => uc.ProgressCalories)
                 .IsRequired()
                 .HasDefaultValue(0);

                e.Property(uc => uc.Completed)
                 .IsRequired()
                 .HasDefaultValue(false);

                e.HasIndex(uc => new { uc.UserId, uc.ChallengeId })
                 .IsUnique()
                 .HasDatabaseName("IX_UserChallenges_UserId_ChallengeId");

                e.HasIndex(uc => new { uc.UserId, uc.Completed })
                 .HasDatabaseName("IX_UserChallenges_UserId_Completed");

                e.HasOne(uc => uc.User)
                 .WithMany()
                 .HasForeignKey(uc => uc.UserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(uc => uc.Challenge)
                 .WithMany()
                 .HasForeignKey(uc => uc.ChallengeId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Restrict);

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_UserChallenges_ProgressDistanceMeters_NonNegative", "\"ProgressDistanceMeters\" >= 0");
                    t.HasCheckConstraint("CK_UserChallenges_ProgressCalories_NonNegative", "\"ProgressCalories\" >= 0");
                });
            });

            modelBuilder.Entity<LeaderboardSnapshot>(e =>
            {
                e.HasKey(s => s.Id);
                // Challenge-scoped snapshot unique index: one row per user per challenge per date.
                e.HasIndex(s => new { s.ChallengeId, s.SnapshotDate, s.UserId })
                 .IsUnique()
                 .HasDatabaseName("IX_LeaderboardSnapshots_ChallengeId_SnapshotDate_UserId");
                e.HasIndex(s => new { s.ChallengeId, s.SnapshotDate })
                 .HasDatabaseName("IX_LeaderboardSnapshots_ChallengeId_SnapshotDate");
                // Partial unique index for global snapshots (ChallengeId IS NULL):
                // ensures one row per user per date for the global leaderboard scope.
                e.HasIndex(s => new { s.SnapshotDate, s.UserId })
                 .IsUnique()
                 .HasFilter("\"ChallengeId\" IS NULL")
                 .HasDatabaseName("IX_LeaderboardSnapshots_Global_SnapshotDate_UserId");
            });

            // TERRITORY (HLN-8 claim/defend ownership)
            modelBuilder.Entity<Territory>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Name).IsRequired().HasMaxLength(200);
                e.Property(t => t.Description).HasMaxLength(1000);
                e.Property(t => t.RegionCode).HasMaxLength(50);
                e.Property(t => t.IconUrl).HasMaxLength(500);
                e.Property(t => t.GeometryCells).HasColumnType("jsonb");
                e.Property(t => t.PublicId).IsRequired();
                e.Property(t => t.CurrentOwnerScoreSnapshot).HasPrecision(12, 4);
                e.Property(t => t.Version).HasDefaultValue(0).IsConcurrencyToken();
                e.HasOne(t => t.CurrentOwnerUser)
                 .WithMany()
                 .HasForeignKey(t => t.CurrentOwnerUserId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(t => t.CurrentOwnerUserId).HasDatabaseName("IX_Territories_CurrentOwnerUserId");
                e.HasIndex(t => t.PublicId).IsUnique().HasDatabaseName("IX_Territories_PublicId");
                e.ToTable("Territories");
            });

            modelBuilder.Entity<TerritoryCell>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.H3Index).IsRequired().HasMaxLength(64);
                e.HasOne(c => c.Territory)
                    .WithMany(t => t.TerritoryCells)
                    .HasForeignKey(c => c.TerritoryId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(c => c.TerritoryId).HasDatabaseName("IX_TerritoryCells_TerritoryId");
                e.HasIndex(c => new { c.TerritoryId, c.H3Index })
                    .IsUnique()
                    .HasDatabaseName("IX_TerritoryCells_TerritoryId_H3Index");
                e.ToTable("TerritoryCells");
            });

            // USER TERRITORY (unlock/progress)
            modelBuilder.Entity<UserTerritory>(e =>
            {
                e.HasKey(ut => ut.Id);
                e.Property(ut => ut.Status).HasConversion<int>();
                e.HasOne(ut => ut.User)
                 .WithMany()
                 .HasForeignKey(ut => ut.UserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(ut => ut.Territory)
                 .WithMany(t => t.UserTerritories)
                 .HasForeignKey(ut => ut.TerritoryId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(ut => new { ut.UserId, ut.TerritoryId }).IsUnique().HasDatabaseName("IX_UserTerritories_UserId_TerritoryId");
                e.ToTable("UserTerritories");
            });

            // TERRITORY UNLOCK CONDITION
            modelBuilder.Entity<TerritoryUnlockCondition>(e =>
            {
                e.HasKey(uc => uc.Id);
                e.Property(uc => uc.UnlockType).HasConversion<int>();
                e.HasOne(uc => uc.Territory)
                 .WithMany(t => t.UnlockConditions)
                 .HasForeignKey(uc => uc.TerritoryId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(uc => uc.TerritoryId).HasDatabaseName("IX_TerritoryUnlockConditions_TerritoryId");
                e.ToTable("TerritoryUnlockConditions");
            });

            // ACHIEVEMENT EVENTS (BE-5 — domain/audit feed for all achievement milestones)
            modelBuilder.Entity<AchievementEvent>(e =>
            {
                e.HasKey(ae => ae.Id);

                e.Property(ae => ae.Type)
                 .HasConversion<int>()
                 .IsRequired();

                e.Property(ae => ae.OccurredAt)
                 .IsRequired();

                e.Property(ae => ae.DeduplicationKey)
                 .IsRequired()
                 .HasMaxLength(500);

                e.Property(ae => ae.Metadata)
                 .HasColumnType("jsonb");

                // Unique constraint is the idempotency gate — duplicate domain events are silently dropped.
                e.HasIndex(ae => ae.DeduplicationKey)
                 .IsUnique()
                 .HasDatabaseName("UX_AchievementEvents_DeduplicationKey");

                e.HasIndex(ae => new { ae.UserId, ae.OccurredAt })
                 .HasDatabaseName("IX_AchievementEvents_UserId_OccurredAt");

                e.HasIndex(ae => ae.UserId)
                 .HasFilter("\"ReadAtUtc\" IS NULL")
                 .HasDatabaseName("IX_AchievementEvents_UserId_Unread");

                e.HasIndex(ae => ae.Type)
                 .HasDatabaseName("IX_AchievementEvents_Type");

                e.HasOne(ae => ae.User)
                 .WithMany()
                 .HasForeignKey(ae => ae.UserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("AchievementEvents");
            });

            // TERRITORY OWNERSHIP HISTORY (HLN-8 audit log)
            modelBuilder.Entity<TerritoryOwnershipHistory>(e =>
            {
                e.HasKey(h => h.Id);
                e.Property(h => h.ActionType).HasConversion<int>();
                e.Property(h => h.ActionScore).HasPrecision(12, 4);
                e.Property(h => h.Metadata).HasColumnType("jsonb");
                e.HasOne(h => h.Territory)
                 .WithMany(t => t.OwnershipHistory)
                 .HasForeignKey(h => h.TerritoryId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(h => h.PreviousOwnerUser)
                 .WithMany()
                 .HasForeignKey(h => h.PreviousOwnerUserId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(h => h.NewOwnerUser)
                 .WithMany()
                 .HasForeignKey(h => h.NewOwnerUserId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(h => h.ActionRun)
                 .WithMany()
                 .HasForeignKey(h => h.ActionRunId)
                 .IsRequired()
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(h => new { h.TerritoryId, h.ActionRunId }).IsUnique().HasDatabaseName("UQ_territory_ownership_history_territory_run");
                e.HasIndex(h => new { h.TerritoryId, h.ActionAt }).HasDatabaseName("IX_territory_ownership_history_territory_action_at");
                e.HasIndex(h => h.ActionRunId).HasDatabaseName("IX_territory_ownership_history_action_run_id");
                e.HasIndex(h => new { h.NewOwnerUserId, h.ActionAt }).HasDatabaseName("IX_territory_ownership_history_new_owner_action_at");
                e.ToTable("territory_ownership_history");
            });
        }
    }
}