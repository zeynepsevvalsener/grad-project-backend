using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_AchievementEvents_ReadAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAtUtc",
                table: "AchievementEvents",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchievementEvents_UserId_Unread",
                table: "AchievementEvents",
                column: "UserId",
                filter: "\"ReadAtUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AchievementEvents_UserId_Unread",
                table: "AchievementEvents");

            migrationBuilder.DropColumn(
                name: "ReadAtUtc",
                table: "AchievementEvents");
        }
    }
}
