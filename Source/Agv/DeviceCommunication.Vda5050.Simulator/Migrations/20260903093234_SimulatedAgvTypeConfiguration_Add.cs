#pragma warning disable 1591, 0472
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.Vda5050Simulator.Migrations
{
    /// <inheritdoc />
    public partial class SimulatedAgvTypeConfiguration_Add : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceCommunicationVda5050SimulatorAgvTypeConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgvTypeIdentifier = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    NoOfAgvs = table.Column<int>(type: "int", nullable: false),
                    StateUpdateIntervalMs = table.Column<int>(type: "int", nullable: false),
                    VisualizationUpdateIntervalMs = table.Column<int>(type: "int", nullable: false),
                    BatteryDischargeRate = table.Column<double>(type: "float", nullable: false),
                    BatteryChargeRate = table.Column<double>(type: "float", nullable: false),
                    InitialBatteryCharge = table.Column<int>(type: "int", nullable: false),
                    InitialBatteryChargeDeviation = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommunicationVda5050SimulatorAgvTypeConfiguration", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommunicationVda5050SimulatorAgvTypeConfiguration_AgvTypeIdentifier",
                table: "DeviceCommunicationVda5050SimulatorAgvTypeConfiguration",
                column: "AgvTypeIdentifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceCommunicationVda5050SimulatorAgvTypeConfiguration");
        }
    }
}
#pragma warning restore 1591, 0472
