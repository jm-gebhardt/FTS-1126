#pragma warning disable 1591, 0472
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.PrismatCommunication.Migrations
{
    /// <inheritdoc />
    public partial class StationNodeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Node",
                table: "PrismatCommunicationStations");

            migrationBuilder.AddColumn<Guid>(
                name: "NodeId",
                table: "PrismatCommunicationStations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "NodeName",
                table: "PrismatCommunicationStations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "PrismatCommunicationStations");

            migrationBuilder.DropColumn(
                name: "NodeName",
                table: "PrismatCommunicationStations");

            migrationBuilder.AddColumn<string>(
                name: "Node",
                table: "PrismatCommunicationStations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }
    }
}
#pragma warning restore 1591, 0472
