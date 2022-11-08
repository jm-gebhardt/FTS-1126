#pragma warning disable 1591, 0472
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Migrations
{
    public partial class Update_OrderSaga_RenamedOrderToOrderDetails_AddedCreationDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderSaga_Order");

            migrationBuilder.CreateTable(
                name: "OrderSaga_OrderDetails",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Creator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSaga_OrderDetails", x => x.CorrelationId);
                    table.ForeignKey(
                        name: "FK_OrderSaga_OrderDetails_OrderSaga_CorrelationId",
                        column: x => x.CorrelationId,
                        principalTable: "OrderSaga",
                        principalColumn: "CorrelationId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderSaga_OrderDetails");

            migrationBuilder.CreateTable(
                name: "OrderSaga_Order",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Creator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSaga_Order", x => x.CorrelationId);
                    table.ForeignKey(
                        name: "FK_OrderSaga_Order_OrderSaga_CorrelationId",
                        column: x => x.CorrelationId,
                        principalTable: "OrderSaga",
                        principalColumn: "CorrelationId",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
#pragma warning restore 1591, 0472
