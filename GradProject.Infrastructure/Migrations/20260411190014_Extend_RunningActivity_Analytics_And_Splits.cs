using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Extend_RunningActivity_Analytics_And_Splits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageCadenceRpm",
                table: "RunningActivities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "RunningActivities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ElevHighMeters",
                table: "RunningActivities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ElevLowMeters",
                table: "RunningActivities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasHeartrate",
                table: "RunningActivities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Kilojoules",
                table: "RunningActivities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxHeartRate",
                table: "RunningActivities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxSpeedMetersPerSecond",
                table: "RunningActivities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteMetadataJson",
                table: "RunningActivities",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SufferScore",
                table: "RunningActivities",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RunningActivityAnalytics",
                columns: table => new
                {
                    RunningActivityId = table.Column<int>(type: "integer", nullable: false),
                    PaceVariabilitySecondsPerKm = table.Column<double>(type: "double precision", nullable: true),
                    FirstHalfPaceSecondsPerKm = table.Column<double>(type: "double precision", nullable: true),
                    SecondHalfPaceSecondsPerKm = table.Column<double>(type: "double precision", nullable: true),
                    IsNegativeSplit = table.Column<bool>(type: "boolean", nullable: true),
                    MovingTimeRatio = table.Column<double>(type: "double precision", nullable: true),
                    ElevationSummaryJson = table.Column<string>(type: "jsonb", nullable: true),
                    PerformanceInsightsJson = table.Column<string>(type: "jsonb", nullable: true),
                    ComputedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunningActivityAnalytics", x => x.RunningActivityId);
                    table.ForeignKey(
                        name: "FK_RunningActivityAnalytics_RunningActivities_RunningActivityId",
                        column: x => x.RunningActivityId,
                        principalTable: "RunningActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunningActivitySplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunningActivityId = table.Column<int>(type: "integer", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    MovingTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    ElapsedTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    ElevationDifferenceMeters = table.Column<double>(type: "double precision", nullable: false),
                    AverageSpeedMetersPerSecond = table.Column<double>(type: "double precision", nullable: false),
                    PaceSecondsPerKm = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunningActivitySplits", x => x.Id);
                    table.CheckConstraint("CK_RunningActivitySplits_DistanceMeters_NonNegative", "\"DistanceMeters\" >= 0");
                    table.CheckConstraint("CK_RunningActivitySplits_PaceSecondsPerKm_NonNegative", "\"PaceSecondsPerKm\" >= 0");
                    table.ForeignKey(
                        name: "FK_RunningActivitySplits_RunningActivities_RunningActivityId",
                        column: x => x.RunningActivityId,
                        principalTable: "RunningActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunningActivitySplits_ActivityId_Ordinal",
                table: "RunningActivitySplits",
                columns: new[] { "RunningActivityId", "Ordinal" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunningActivityAnalytics");

            migrationBuilder.DropTable(
                name: "RunningActivitySplits");

            migrationBuilder.DropColumn(
                name: "AverageCadenceRpm",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "ElevHighMeters",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "ElevLowMeters",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "HasHeartrate",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "Kilojoules",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "MaxHeartRate",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "MaxSpeedMetersPerSecond",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "RouteMetadataJson",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "SufferScore",
                table: "RunningActivities");
        }
    }
}
