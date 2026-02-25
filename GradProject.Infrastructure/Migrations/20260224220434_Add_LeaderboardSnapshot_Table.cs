using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_LeaderboardSnapshot_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaderboardSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    TerritoryScore = table.Column<double>(type: "double precision", nullable: true),
                    TotalDistance = table.Column<long>(type: "bigint", nullable: false),
                    AveragePace = table.Column<double>(type: "double precision", nullable: false),
                    CompletionSpeed = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardSnapshots_ChallengeId_SnapshotDate",
                table: "LeaderboardSnapshots",
                columns: new[] { "ChallengeId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardSnapshots_ChallengeId_SnapshotDate_UserId",
                table: "LeaderboardSnapshots",
                columns: new[] { "ChallengeId", "SnapshotDate", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaderboardSnapshots");
        }
    }
}
