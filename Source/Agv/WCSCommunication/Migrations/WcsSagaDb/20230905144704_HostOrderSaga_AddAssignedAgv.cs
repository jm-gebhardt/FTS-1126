using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Migrations.WcsSagaDb
{
    /// <inheritdoc />
    public partial class HostOrderSagaAddAssignedAgv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedAgv",
                table: "HostOrderSaga",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedAgv",
                table: "HostOrderSaga");
        }
    }
}
