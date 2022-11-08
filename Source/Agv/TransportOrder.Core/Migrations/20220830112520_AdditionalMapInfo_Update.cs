#pragma warning disable 1591, 0472
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Migrations
{
    public partial class AdditionalMapInfo_Update : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModificationDateTime",
                table: "MapTable",
                newName: "ModificationDate");

            migrationBuilder.AddColumn<double>(
                name: "Resolution",
                table: "MapTable",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "XOffset",
                table: "MapTable",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YOffset",
                table: "MapTable",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "MapTable");

            migrationBuilder.DropColumn(
                name: "XOffset",
                table: "MapTable");

            migrationBuilder.DropColumn(
                name: "YOffset",
                table: "MapTable");

            migrationBuilder.RenameColumn(
                name: "ModificationDate",
                table: "MapTable",
                newName: "ModificationDateTime");
        }
    }
}
#pragma warning restore 1591, 0472
