using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.WCSCommunication.Migrations.WcsCommunicationDb
{
    /// <inheritdoc />
    public partial class HostOrderAddTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Cancelled",
                table: "HostOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Failed",
                table: "HostOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Finished",
                table: "HostOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Started",
                table: "HostOrder",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cancelled",
                table: "HostOrder");

            migrationBuilder.DropColumn(
                name: "Failed",
                table: "HostOrder");

            migrationBuilder.DropColumn(
                name: "Finished",
                table: "HostOrder");

            migrationBuilder.DropColumn(
                name: "Started",
                table: "HostOrder");
        }
    }
}
