using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradProject.Infrastructure.Migrations
{
    public partial class Fix_AddMealId_To_ConsumedFoods : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MealId",
                table: "ConsumedFoods",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedFoods_MealId",
                table: "ConsumedFoods",
                column: "MealId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumedFoods_Meals_MealId",
                table: "ConsumedFoods",
                column: "MealId",
                principalTable: "Meals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumedFoods_Meals_MealId",
                table: "ConsumedFoods");

            migrationBuilder.DropIndex(
                name: "IX_ConsumedFoods_MealId",
                table: "ConsumedFoods");

            migrationBuilder.DropColumn(
                name: "MealId",
                table: "ConsumedFoods");
        }
    }
}