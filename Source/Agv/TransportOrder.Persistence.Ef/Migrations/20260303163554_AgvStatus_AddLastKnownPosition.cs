#pragma warning disable 1591, 0472
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AgvStatus_AddLastKnownPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "LastKnownDeviationRange",
                table: "AgvStatus",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastKnownPositionInitialized",
                table: "AgvStatus",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "LastKnownTheta",
                table: "AgvStatus",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "LastKnownX",
                table: "AgvStatus",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "LastKnownY",
                table: "AgvStatus",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "LastKnownZ",
                table: "AgvStatus",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastKnownDeviationRange",
                table: "AgvStatus");

            migrationBuilder.DropColumn(
                name: "LastKnownPositionInitialized",
                table: "AgvStatus");

            migrationBuilder.DropColumn(
                name: "LastKnownTheta",
                table: "AgvStatus");

            migrationBuilder.DropColumn(
                name: "LastKnownX",
                table: "AgvStatus");

            migrationBuilder.DropColumn(
                name: "LastKnownY",
                table: "AgvStatus");

            migrationBuilder.DropColumn(
                name: "LastKnownZ",
                table: "AgvStatus");
        }
    }
}
#pragma warning restore 1591, 0472
