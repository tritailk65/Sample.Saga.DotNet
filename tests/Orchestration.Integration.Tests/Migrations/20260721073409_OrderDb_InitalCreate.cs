using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchestration.Integration.Tests.Migrations
{
    /// <inheritdoc />
    public partial class OrderDb_InitalCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentState = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponseAddress = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.CorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "GoodViewModel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    OrderSagaCorrelationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodViewModel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodViewModel_Orders_OrderSagaCorrelationId",
                        column: x => x.OrderSagaCorrelationId,
                        principalTable: "Orders",
                        principalColumn: "CorrelationId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodViewModel_OrderSagaCorrelationId",
                table: "GoodViewModel",
                column: "OrderSagaCorrelationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodViewModel");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
