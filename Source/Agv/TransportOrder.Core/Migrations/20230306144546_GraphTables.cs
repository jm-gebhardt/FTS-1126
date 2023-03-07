#pragma warning disable 1591, 0472
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Migrations
{
    /// <inheritdoc />
    public partial class GraphTables : Migration
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
                    AgvOfflineTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Settings = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgvSaga", x => x.CorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "Configuration",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ConfigurationType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuration", x => x.Key);
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
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false),
                    Theta = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
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
                    FailedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedAgvId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSaga", x => x.CorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "Edges",
                columns: table => new
                {
                    FromId = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    ToId = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Metric = table.Column<float>(type: "real", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Edges", x => new { x.FromId, x.ToId });
                    table.ForeignKey(
                        name: "FK_Edges_Nodes_FromId",
                        column: x => x.FromId,
                        principalTable: "Nodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Edges_Nodes_ToId",
                        column: x => x.ToId,
                        principalTable: "Nodes",
                        principalColumn: "Id");
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

            migrationBuilder.CreateIndex(
                name: "IX_Edges_ToId",
                table: "Edges",
                column: "ToId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgvSaga");

            migrationBuilder.DropTable(
                name: "Configuration");

            migrationBuilder.DropTable(
                name: "Edges");

            migrationBuilder.DropTable(
                name: "Map");

            migrationBuilder.DropTable(
                name: "OrderChangedEvent");

            migrationBuilder.DropTable(
                name: "OrderSaga_OrderDetails");

            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropTable(
                name: "OrderSaga");
        }
    }
}
#pragma warning restore 1591, 0472
