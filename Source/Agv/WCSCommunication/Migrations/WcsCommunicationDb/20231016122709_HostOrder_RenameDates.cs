using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.WCSCommunication.Migrations.WcsCommunicationDb
{
    /// <inheritdoc />
    public partial class HostOrderRenameDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Started",
                table: "HostOrder",
                newName: "StartedDate");

            migrationBuilder.RenameColumn(
                name: "Finished",
                table: "HostOrder",
                newName: "FinishedDate");

            migrationBuilder.RenameColumn(
                name: "Failed",
                table: "HostOrder",
                newName: "FailedDate");

            migrationBuilder.RenameColumn(
                name: "Cancelled",
                table: "HostOrder",
                newName: "CancelledDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartedDate",
                table: "HostOrder",
                newName: "Started");

            migrationBuilder.RenameColumn(
                name: "FinishedDate",
                table: "HostOrder",
                newName: "Finished");

            migrationBuilder.RenameColumn(
                name: "FailedDate",
                table: "HostOrder",
                newName: "Failed");

            migrationBuilder.RenameColumn(
                name: "CancelledDate",
                table: "HostOrder",
                newName: "Cancelled");
        }
    }
}
