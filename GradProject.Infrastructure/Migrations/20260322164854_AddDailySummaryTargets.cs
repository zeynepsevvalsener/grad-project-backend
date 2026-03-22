using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailySummaryTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalorieTarget",
                table: "DailySummaries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CarbTargetG",
                table: "DailySummaries",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FatTargetG",
                table: "DailySummaries",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProteinTargetG",
                table: "DailySummaries",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalorieTarget",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "CarbTargetG",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "FatTargetG",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "ProteinTargetG",
                table: "DailySummaries");
        }
    }
}