using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Migrations
{
    /// <inheritdoc />
    public partial class Initialize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceCommunicationAgv",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommunicationAgv", x => x.CorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCommunicationKarisOrderMapping",
                columns: table => new
                {
                    VehicleOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InternalOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommunicationKarisOrderMapping", x => x.VehicleOrderId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCommunicationMqttEdge",
                columns: table => new
                {
                    InternalEdge = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EdgeId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StartId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EndId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Bidirectional = table.Column<bool>(type: "bit", nullable: false),
                    MaxVelocity = table.Column<float>(type: "real", nullable: false),
                    Attributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EdgeType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ControlPoints = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExitAngle = table.Column<float>(type: "real", nullable: false),
                    ExitDistance = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommunicationMqttEdge", x => x.InternalEdge);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCommunicationMqttNode",
                columns: table => new
                {
                    NodeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommunicationMqttNode", x => x.NodeId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceCommunicationAgv");

            migrationBuilder.DropTable(
                name: "DeviceCommunicationKarisOrderMapping");

            migrationBuilder.DropTable(
                name: "DeviceCommunicationMqttEdge");

            migrationBuilder.DropTable(
                name: "DeviceCommunicationMqttNode");
        }
    }
}
