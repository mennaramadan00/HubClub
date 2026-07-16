using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubClub.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageRevenueToDailyClosingg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExcelBackupPath",
                table: "daily_closings",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPackageRevenue",
                table: "daily_closings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_daily_closings_BusinessDate",
                table: "daily_closings",
                column: "BusinessDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_daily_closings_BusinessDate",
                table: "daily_closings");

            migrationBuilder.DropColumn(
                name: "TotalPackageRevenue",
                table: "daily_closings");

            migrationBuilder.UpdateData(
                table: "daily_closings",
                keyColumn: "ExcelBackupPath",
                keyValue: null,
                column: "ExcelBackupPath",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "ExcelBackupPath",
                table: "daily_closings",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
