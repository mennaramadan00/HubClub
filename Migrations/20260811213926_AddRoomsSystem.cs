using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubClub.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "RoomSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomSessions_CustomerId",
                table: "RoomSessions",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoomSessions_customers_CustomerId",
                table: "RoomSessions",
                column: "CustomerId",
                principalTable: "customers",
                principalColumn: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoomSessions_customers_CustomerId",
                table: "RoomSessions");

            migrationBuilder.DropIndex(
                name: "IX_RoomSessions_CustomerId",
                table: "RoomSessions");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "RoomSessions");
        }
    }
}
