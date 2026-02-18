using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_ProgressCalories_To_UserChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgressCalories",
                table: "UserChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserChallenges_ProgressCalories_NonNegative",
                table: "UserChallenges",
                sql: "\"ProgressCalories\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserChallenges_ProgressCalories_NonNegative",
                table: "UserChallenges");

            migrationBuilder.DropColumn(
                name: "ProgressCalories",
                table: "UserChallenges");
        }
    }
}
