using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIdMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceCommunicationKarisOrderMapping");
        }
    }
}
