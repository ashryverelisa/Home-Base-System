using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBase.Database.Migrations
{
    /// <inheritdoc />
    public partial class RecipeReviewAndPlanRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "plan_from",
                table: "shopping_list_item",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "plan_to",
                table: "shopping_list_item",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "needs_review",
                table: "recipe_ingredient",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "plan_from",
                table: "shopping_list_item");

            migrationBuilder.DropColumn(
                name: "plan_to",
                table: "shopping_list_item");

            migrationBuilder.DropColumn(
                name: "needs_review",
                table: "recipe_ingredient");
        }
    }
}
