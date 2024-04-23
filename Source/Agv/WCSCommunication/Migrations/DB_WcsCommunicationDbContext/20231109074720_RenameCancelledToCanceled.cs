using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.WCSCommunication.Migrations.DBWcsCommunicationDbContext
{
    /// <inheritdoc />
    public partial class RenameCancelledToCanceled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CancelledDate",
                table: "HostOrder",
                newName: "CanceledDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "HostOrder",
                newName: "CancelledDate");
        }
    }
}
