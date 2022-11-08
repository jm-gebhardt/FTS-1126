#pragma warning disable 1591, 0472
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Migrations
{
    public partial class Maps_RenameTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MapTable",
                table: "MapTable");

            migrationBuilder.RenameTable(
                name: "MapTable",
                newName: "Map");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Map",
                table: "Map",
                column: "CorrelationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Map",
                table: "Map");

            migrationBuilder.RenameTable(
                name: "Map",
                newName: "MapTable");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MapTable",
                table: "MapTable",
                column: "CorrelationId");
        }
    }
}
#pragma warning restore 1591, 0472
