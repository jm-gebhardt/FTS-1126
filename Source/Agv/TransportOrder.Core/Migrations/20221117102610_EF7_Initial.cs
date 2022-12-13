#pragma warning disable 1591, 0472
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Migrations
{
    /// <inheritdoc />
    public partial class EF7Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgvSaga",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgvId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AgvStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgvGraph = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgvData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgvDataRequestedLastTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgvGraphRequestedLastTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Alive = table.Column<bool>(type: "bit", nullable: false),
                    AgvOfflineTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgvSaga", x => x.CorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "Map",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MapId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    MapValue = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ModificationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Resolution = table.Column<double>(type: "float", nullable: false),
                    XOffset = table.Column<double>(type: "float", nullable: false),
                    YOffset = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Map", x => x.CorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "OrderChangedEvent",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderSaga = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Hook = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderChangedEvent", x => x.CorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "OrderSaga",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AssignedAgv = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FailedReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSaga", x => x.CorrelationId);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgvSaga");

            migrationBuilder.DropTable(
                name: "Map");

            migrationBuilder.DropTable(
                name: "OrderChangedEvent");

            migrationBuilder.DropTable(
                name: "OrderSaga_OrderDetails");

            migrationBuilder.DropTable(
                name: "OrderSaga");
        }
    }
}
#pragma warning restore 1591, 0472
