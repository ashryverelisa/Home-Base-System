using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBase.Database.Migrations
{
    /// <inheritdoc />
    public partial class AssetServiceDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "next_service_at",
                table: "asset",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "service_interval_days",
                table: "asset",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "next_service_at",
                table: "asset");

            migrationBuilder.DropColumn(
                name: "service_interval_days",
                table: "asset");
        }
    }
}
