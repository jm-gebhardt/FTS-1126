#pragma warning disable 1591, 0472
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Migrations
{
    public partial class AgvSaga_Update : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "AssignedAgv",
                table: "OrderSaga",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AgvId",
                table: "AgvSaga",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgvGraph",
                table: "AgvSaga",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgvStatus",
                table: "AgvSaga",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgvGraph",
                table: "AgvSaga");

            migrationBuilder.DropColumn(
                name: "AgvStatus",
                table: "AgvSaga");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedAgv",
                table: "OrderSaga",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AgvId",
                table: "AgvSaga",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
#pragma warning restore 1591, 0472
