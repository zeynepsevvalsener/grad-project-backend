using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Territory_Ownership_And_History : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Territories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RegionCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OwnershipTargetPercent = table.Column<int>(type: "integer", nullable: false),
                    GeometryCells = table.Column<string>(type: "jsonb", nullable: true),
                    CurrentOwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    CurrentOwnerSince = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CurrentOwnerScoreSnapshot = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Territories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Territories_Users_CurrentOwnerUserId",
                        column: x => x.CurrentOwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "territory_ownership_history",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TerritoryId = table.Column<int>(type: "integer", nullable: false),
                    PreviousOwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    NewOwnerUserId = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    ActionRunId = table.Column<int>(type: "integer", nullable: false),
                    ActionScore = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    ActionAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territory_ownership_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_territory_ownership_history_RunningActivities_ActionRunId",
                        column: x => x.ActionRunId,
                        principalTable: "RunningActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territory_ownership_history_Territories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "Territories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_territory_ownership_history_Users_NewOwnerUserId",
                        column: x => x.NewOwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_territory_ownership_history_Users_PreviousOwnerUserId",
                        column: x => x.PreviousOwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TerritoryUnlockConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TerritoryId = table.Column<int>(type: "integer", nullable: false),
                    UnlockType = table.Column<int>(type: "integer", nullable: false),
                    TargetValue = table.Column<double>(type: "double precision", nullable: false),
                    RelatedEntityId = table.Column<int>(type: "integer", nullable: true),
                    RequiresAll = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerritoryUnlockConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerritoryUnlockConditions_Territories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "Territories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTerritories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TerritoryId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    OwnedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTerritories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTerritories_Territories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "Territories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTerritories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Territories_CurrentOwnerUserId",
                table: "Territories",
                column: "CurrentOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_territory_ownership_history_action_run_id",
                table: "territory_ownership_history",
                column: "ActionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_territory_ownership_history_new_owner_action_at",
                table: "territory_ownership_history",
                columns: new[] { "NewOwnerUserId", "ActionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_territory_ownership_history_PreviousOwnerUserId",
                table: "territory_ownership_history",
                column: "PreviousOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_territory_ownership_history_territory_action_at",
                table: "territory_ownership_history",
                columns: new[] { "TerritoryId", "ActionAt" });

            migrationBuilder.CreateIndex(
                name: "UQ_territory_ownership_history_territory_run",
                table: "territory_ownership_history",
                columns: new[] { "TerritoryId", "ActionRunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryUnlockConditions_TerritoryId",
                table: "TerritoryUnlockConditions",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTerritories_TerritoryId",
                table: "UserTerritories",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTerritories_UserId_TerritoryId",
                table: "UserTerritories",
                columns: new[] { "UserId", "TerritoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "territory_ownership_history");

            migrationBuilder.DropTable(
                name: "TerritoryUnlockConditions");

            migrationBuilder.DropTable(
                name: "UserTerritories");

            migrationBuilder.DropTable(
                name: "Territories");
        }
    }
}
