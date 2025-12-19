using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_RunningActivities_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RunningActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ExternalActivityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    MovingTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    ElapsedTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    TotalElevationGain = table.Column<double>(type: "double precision", nullable: false),
                    AverageSpeed = table.Column<double>(type: "double precision", nullable: false),
                    AverageHeartRate = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunningActivities", x => x.Id);
                    table.CheckConstraint("CK_RunningActivities_AverageSpeed_NonNegative", "\"AverageSpeed\" >= 0");
                    table.CheckConstraint("CK_RunningActivities_DistanceMeters_NonNegative", "\"DistanceMeters\" >= 0");
                    table.CheckConstraint("CK_RunningActivities_ElapsedTimeSeconds_NonNegative", "\"ElapsedTimeSeconds\" >= 0");
                    table.CheckConstraint("CK_RunningActivities_MovingTimeSeconds_NonNegative", "\"MovingTimeSeconds\" >= 0");
                    table.CheckConstraint("CK_RunningActivities_TotalElevationGain_NonNegative", "\"TotalElevationGain\" >= 0");
                    table.ForeignKey(
                        name: "FK_RunningActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunningActivities_StartTime",
                table: "RunningActivities",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_RunningActivities_UserId",
                table: "RunningActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RunningActivities_UserId_ExternalActivityId",
                table: "RunningActivities",
                columns: new[] { "UserId", "ExternalActivityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunningActivities");
        }
    }
}
