using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "en");

            // Safety: if the column already existed or was added earlier with empty default,
            // make sure existing users have a valid language.
            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "Language" = 'en'
                WHERE "Language" IS NULL OR "Language" = '';
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "Users");
        }
    }
}