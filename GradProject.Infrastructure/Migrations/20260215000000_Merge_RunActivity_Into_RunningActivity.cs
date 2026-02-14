using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Merge_RunActivity_Into_RunningActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Add new columns to RunningActivities
            migrationBuilder.AddColumn<DateOnly>(
                name: "RunDate",
                table: "RunningActivities",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "BurnedCalories",
                table: "RunningActivities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "RunningActivities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "STRAVA");

            // 2) Backfill RunDate from StartTime for existing rows
            migrationBuilder.Sql(
                "UPDATE \"RunningActivities\" SET \"RunDate\" = \"StartTime\"::date WHERE \"RunDate\" = '0001-01-01'");

            // 3) Add index on RunDate
            migrationBuilder.CreateIndex(
                name: "IX_RunningActivities_RunDate",
                table: "RunningActivities",
                column: "RunDate");

            // 4) Drop RunActivities table (indexes & constraints dropped automatically)
            migrationBuilder.DropTable(
                name: "RunActivities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-create RunActivities table
            migrationBuilder.CreateTable(
                name: "RunActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RunDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    DistanceMeters = table.Column<float>(type: "real", nullable: false),
                    BurnedCalories = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunActivities", x => x.Id);
                    table.CheckConstraint("CK_RunActivities_DistanceMeters_NonNegative", "\"DistanceMeters\" >= 0");
                    table.CheckConstraint("CK_RunActivities_DurationSeconds_NonNegative", "\"DurationSeconds\" >= 0");
                    table.ForeignKey(
                        name: "FK_RunActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunActivities_RunDate",
                table: "RunActivities",
                column: "RunDate");

            migrationBuilder.CreateIndex(
                name: "IX_RunActivities_UserId",
                table: "RunActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RunActivities_UserId_ExternalId",
                table: "RunActivities",
                columns: new[] { "UserId", "ExternalId" },
                unique: true);

            // Drop new columns from RunningActivities
            migrationBuilder.DropIndex(
                name: "IX_RunningActivities_RunDate",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "RunDate",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "BurnedCalories",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "RunningActivities");
        }
    }
}

