using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Migrations.DB_TransportOrderDbContext
{
    /// <inheritdoc />
    public partial class AgvData_SerialNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgvId",
                table: "Agv");

            migrationBuilder.AddColumn<string>(
                name: "SoftwareVersion",
                table: "AgvData",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Agv",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Agv",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoftwareVersion",
                table: "AgvData");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Agv");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Agv");

            migrationBuilder.AddColumn<string>(
                name: "AgvId",
                table: "Agv",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
