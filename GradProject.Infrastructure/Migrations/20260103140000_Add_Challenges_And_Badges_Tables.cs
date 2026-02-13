using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Challenges_And_Badges_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Metric = table.Column<int>(type: "integer", nullable: false),
                    TargetValue = table.Column<double>(type: "double precision", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RewardPoints = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                    table.CheckConstraint("CK_Challenges_TargetValue_NonNegative", "\"TargetValue\" >= 0");
                    table.CheckConstraint("CK_Challenges_RewardPoints_NonNegative", "\"RewardPoints\" >= 0");
                    table.CheckConstraint("CK_Challenges_EndDate_GreaterThanOrEqual_StartDate", "\"EndDate\" >= \"StartDate\"");
                });

            migrationBuilder.CreateTable(
                name: "Badges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PointsReward = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.Id);
                    table.CheckConstraint("CK_Badges_PointsReward_NonNegative", "\"PointsReward\" >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_IsActive",
                table: "Challenges",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_StartDate",
                table: "Challenges",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_EndDate",
                table: "Challenges",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Badges_IsActive",
                table: "Badges",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "Badges");
        }
    }
}

