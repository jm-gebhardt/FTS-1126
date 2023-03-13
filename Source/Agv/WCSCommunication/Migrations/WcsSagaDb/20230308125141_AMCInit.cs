// ReSharper disable all
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Migrations.WcsSagaDb
{
    /// <inheritdoc />
    public partial class AMCInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HostOrderSaga",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostOrderSaga", x => x.CorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "HostOrderSaga_OrderDetails",
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
                    table.PrimaryKey("PK_HostOrderSaga_OrderDetails", x => x.CorrelationId);
                    table.ForeignKey(
                        name: "FK_HostOrderSaga_OrderDetails_HostOrderSaga_CorrelationId",
                        column: x => x.CorrelationId,
                        principalTable: "HostOrderSaga",
                        principalColumn: "CorrelationId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostOrderSaga_OrderDetails");

            migrationBuilder.DropTable(
                name: "HostOrderSaga");
        }
    }
}
