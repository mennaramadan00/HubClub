using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubClub.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomsSystemAndStockRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomSessionId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoomPricings",
                columns: table => new
                {
                    RoomPricingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PricePerHour = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomPricings", x => x.RoomPricingId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.RoomId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoomSessions",
                columns: table => new
                {
                    RoomSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    HourlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTimePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalProductPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    IsClosed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomSessions", x => x.RoomSessionId);
                    table.ForeignKey(
                        name: "FK_RoomSessions_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoomSessionProducts",
                columns: table => new
                {
                    RoomSessionProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoomSessionId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPriceAtSale = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomSessionProducts", x => x.RoomSessionProductId);
                    table.ForeignKey(
                        name: "FK_RoomSessionProducts_RoomSessions_RoomSessionId",
                        column: x => x.RoomSessionId,
                        principalTable: "RoomSessions",
                        principalColumn: "RoomSessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomSessionProducts_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_RoomSessionId",
                table: "StockMovements",
                column: "RoomSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SessionId",
                table: "StockMovements",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomSessionProducts_ProductId",
                table: "RoomSessionProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomSessionProducts_RoomSessionId",
                table: "RoomSessionProducts",
                column: "RoomSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomSessions_RoomId",
                table: "RoomSessions",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_RoomSessions_RoomSessionId",
                table: "StockMovements",
                column: "RoomSessionId",
                principalTable: "RoomSessions",
                principalColumn: "RoomSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_sessions_SessionId",
                table: "StockMovements",
                column: "SessionId",
                principalTable: "sessions",
                principalColumn: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_RoomSessions_RoomSessionId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_sessions_SessionId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "RoomPricings");

            migrationBuilder.DropTable(
                name: "RoomSessionProducts");

            migrationBuilder.DropTable(
                name: "RoomSessions");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_RoomSessionId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_SessionId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "RoomSessionId",
                table: "StockMovements");
        }
    }
}
