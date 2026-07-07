#pragma warning disable 1591, 0472
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Migrations
{
    /// <inheritdoc />
    public partial class Add_Agv_AgvTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgvTypeId",
                table: "Agv",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgvTypeId",
                table: "Agv");
        }
    }
}
#pragma warning restore 1591, 0472
