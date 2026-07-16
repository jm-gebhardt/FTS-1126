using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AgvError_ErrorReceived",
                table: "AgvError",
                column: "ErrorReceived");

            migrationBuilder.CreateIndex(
                name: "IX_ActionGroup_CreatedDate",
                table: "ActionGroup",
                column: "CreatedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgvError_ErrorReceived",
                table: "AgvError");

            migrationBuilder.DropIndex(
                name: "IX_ActionGroup_CreatedDate",
                table: "ActionGroup");
        }
    }
}
