using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Strava_Fields_And_RunActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StravaAccessToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StravaAthleteId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StravaConnectedAt",
                table: "Users",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StravaRefreshToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StravaTokenExpiresAt",
                table: "Users",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActivityLevel",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Foods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kcal = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    ProteinG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    FatG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CarbG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    SugarG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    FiberG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    SodiumMg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DefaultPortionG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Aliases = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foods", x => x.Id);
                    table.CheckConstraint("CK_Foods_CarbG_NonNegative", "\"CarbG\" >= 0");
                    table.CheckConstraint("CK_Foods_DefaultPortionG_Positive", "\"DefaultPortionG\" > 0");
                    table.CheckConstraint("CK_Foods_FatG_NonNegative", "\"FatG\" >= 0");
                    table.CheckConstraint("CK_Foods_FiberG_NonNegative", "\"FiberG\" >= 0");
                    table.CheckConstraint("CK_Foods_Kcal_NonNegative", "\"Kcal\" >= 0");
                    table.CheckConstraint("CK_Foods_ProteinG_NonNegative", "\"ProteinG\" >= 0");
                    table.CheckConstraint("CK_Foods_SodiumMg_NonNegative", "\"SodiumMg\" >= 0");
                    table.CheckConstraint("CK_Foods_SugarG_NonNegative", "\"SugarG\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "RunActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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

            migrationBuilder.CreateTable(
                name: "ConsumedFoods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FoodId = table.Column<int>(type: "integer", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PortionG = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumedFoods", x => x.Id);
                    table.CheckConstraint("CK_ConsumedFoods_PortionG_Positive", "\"PortionG\" > 0");
                    table.ForeignKey(
                        name: "FK_ConsumedFoods_Foods_FoodId",
                        column: x => x.FoodId,
                        principalTable: "Foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumedFoods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedFoods_FoodId",
                table: "ConsumedFoods",
                column: "FoodId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedFoods_UserId",
                table: "ConsumedFoods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedFoods_UserId_ConsumedAt",
                table: "ConsumedFoods",
                columns: new[] { "UserId", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Foods_Category",
                table: "Foods",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Foods_Name",
                table: "Foods",
                column: "Name",
                unique: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumedFoods");

            migrationBuilder.DropTable(
                name: "RunActivities");

            migrationBuilder.DropTable(
                name: "Foods");

            migrationBuilder.DropColumn(
                name: "StravaAccessToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StravaAthleteId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StravaConnectedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StravaRefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StravaTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "Profiles");
        }
    }
}
