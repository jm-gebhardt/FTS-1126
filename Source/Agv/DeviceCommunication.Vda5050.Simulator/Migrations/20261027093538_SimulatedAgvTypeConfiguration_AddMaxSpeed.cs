using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.Vda5050Simulator.Migrations
{
    /// <inheritdoc />
    public partial class SimulatedAgvTypeConfiguration_AddMaxSpeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaxSpeed",
                table: "DeviceCommunicationVda5050SimulatorAgvTypeConfiguration",
                type: "float",
                nullable: false,
                defaultValue: 1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxSpeed",
                table: "DeviceCommunicationVda5050SimulatorAgvTypeConfiguration");
        }
    }
}
