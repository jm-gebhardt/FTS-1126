using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Migrations.WcsSagaDb
{
    /// <inheritdoc />
    public partial class HostOrderSagaAddErrors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancellationError",
                table: "HostOrderSaga",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HandlingError",
                table: "HostOrderSaga",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationError",
                table: "HostOrderSaga");

            migrationBuilder.DropColumn(
                name: "HandlingError",
                table: "HostOrderSaga");
        }
    }
}
