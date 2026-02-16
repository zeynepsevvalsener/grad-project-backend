using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Route_To_RunningActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable PostGIS extension if not already enabled
            // NOTE: PostGIS must be installed on the system (brew install postgis)
            // and the extension must be enabled in PostgreSQL (CREATE EXTENSION postgis;)
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");

            migrationBuilder.AddColumn<LineString>(
                name: "Route",
                table: "RunningActivities",
                type: "geometry(LineString, 4326)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Route",
                table: "RunningActivities");

            // Note: We don't drop the PostGIS extension as it might be used by other tables
        }
    }
}
