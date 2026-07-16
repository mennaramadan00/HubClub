using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubClub.Migrations
{
    /// <inheritdoc />
    public partial class addBusinessDayInUserPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE user_packages SET RowVersion = CURRENT_TIMESTAMP WHERE RowVersion < '1970-01-01 00:00:01';");
            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "user_packages",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldRowVersion: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PurchaseBusinessDate",
                table: "user_packages",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseBusinessDate",
                table: "user_packages");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "user_packages",
                type: "datetime(6)",
                rowVersion: true,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true);
        }
    }
}
