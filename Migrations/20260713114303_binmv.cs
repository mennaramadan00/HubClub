using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubClub.Migrations
{
    /// <inheritdoc />
    public partial class binmv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BusinessDate",
                table: "StockMovements",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessDate",
                table: "StockMovements");
        }
    }
}
