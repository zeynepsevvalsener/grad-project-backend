using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_DailySummaries_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailySummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalIntakeCalories = table.Column<int>(type: "integer", nullable: false),
                    TotalProtein = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalCarbs = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalFat = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySummaries", x => x.Id);
                    table.CheckConstraint("CK_DailySummaries_TotalIntakeCalories_NonNegative", "\"TotalIntakeCalories\" >= 0");
                    table.CheckConstraint("CK_DailySummaries_TotalProtein_NonNegative", "\"TotalProtein\" >= 0");
                    table.CheckConstraint("CK_DailySummaries_TotalCarbs_NonNegative", "\"TotalCarbs\" >= 0");
                    table.CheckConstraint("CK_DailySummaries_TotalFat_NonNegative", "\"TotalFat\" >= 0");
                    table.ForeignKey(
                        name: "FK_DailySummaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_Date",
                table: "DailySummaries",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_UserId",
                table: "DailySummaries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_UserId_Date",
                table: "DailySummaries",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailySummaries");
        }
    }
}

