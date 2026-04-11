using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Challenge_Custom_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Challenges",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Challenges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "Challenges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_CreatedByUserId",
                table: "Challenges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_IsCustom_IsActive",
                table: "Challenges",
                columns: new[] { "IsCustom", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Users_CreatedByUserId",
                table: "Challenges",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Users_CreatedByUserId",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_CreatedByUserId",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_IsCustom_IsActive",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "Challenges");
        }
    }
}
