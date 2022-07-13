using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Migrations
{
    public partial class WCSInit : Migration
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="migrationBuilder"></param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FromAgv",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordType = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    MessageStatus = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    LeNo = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Vehicle = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Creator = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CcuVersion = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FromAgv", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToAgv",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordType = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    MessageStatus = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    LeNo = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Vehicle = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Creator = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CcuVersion = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToAgv", x => x.Id);
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="migrationBuilder"></param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FromAgv");

            migrationBuilder.DropTable(
                name: "ToAgv");
        }
    }
}
