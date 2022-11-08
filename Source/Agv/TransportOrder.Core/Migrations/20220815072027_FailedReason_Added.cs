#pragma warning disable 1591, 0472
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Migrations
{
    public partial class FailedReason_Added : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailedReason",
                table: "OrderSaga",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedReason",
                table: "OrderSaga");
        }
    }
}
#pragma warning restore 1591, 0472
