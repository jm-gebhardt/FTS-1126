#pragma warning disable 1591, 0472
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Migrations
{
    public partial class DateTime_AgvDataAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AgvDataReceivedLastTime",
                table: "AgvSaga",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AgvGraphReceivedLastTime",
                table: "AgvSaga",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgvDataReceivedLastTime",
                table: "AgvSaga");

            migrationBuilder.DropColumn(
                name: "AgvGraphReceivedLastTime",
                table: "AgvSaga");
        }
    }
}
#pragma warning restore 1591, 0472
