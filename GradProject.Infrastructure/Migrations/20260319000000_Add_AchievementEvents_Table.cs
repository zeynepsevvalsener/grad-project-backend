using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_AchievementEvents_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchievementEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BadgeId = table.Column<int>(type: "integer", nullable: true),
                    ChallengeId = table.Column<int>(type: "integer", nullable: true),
                    TerritoryId = table.Column<int>(type: "integer", nullable: true),
                    RunId = table.Column<int>(type: "integer", nullable: true),
                    PreviousRank = table.Column<int>(type: "integer", nullable: true),
                    CurrentRank = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    DeduplicationKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchievementEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEvents_Type",
                table: "AchievementEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEvents_UserId_OccurredAt",
                table: "AchievementEvents",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "UX_AchievementEvents_DeduplicationKey",
                table: "AchievementEvents",
                column: "DeduplicationKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchievementEvents");
        }
    }
}
