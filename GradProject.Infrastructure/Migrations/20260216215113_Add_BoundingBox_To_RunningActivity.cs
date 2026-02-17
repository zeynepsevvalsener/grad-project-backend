using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_BoundingBox_To_RunningActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaxLat",
                table: "RunningActivities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxLng",
                table: "RunningActivities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinLat",
                table: "RunningActivities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinLng",
                table: "RunningActivities",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxLat",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "MaxLng",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "MinLat",
                table: "RunningActivities");

            migrationBuilder.DropColumn(
                name: "MinLng",
                table: "RunningActivities");
        }
    }
}
