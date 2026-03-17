using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Make_LeaderboardSnapshot_ChallengeId_Nullable_For_Global_Scope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Version",
                table: "Territories",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ChallengeId",
                table: "LeaderboardSnapshots",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardSnapshots_Global_SnapshotDate_UserId",
                table: "LeaderboardSnapshots",
                columns: new[] { "SnapshotDate", "UserId" },
                unique: true,
                filter: "\"ChallengeId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaderboardSnapshots_Global_SnapshotDate_UserId",
                table: "LeaderboardSnapshots");

            migrationBuilder.AlterColumn<int>(
                name: "Version",
                table: "Territories",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ChallengeId",
                table: "LeaderboardSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
