using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubClub.Migrations
{
    /// <inheritdoc />
    public partial class ColumnToUserPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "user_packages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "user_packages");
        }
    }
}
