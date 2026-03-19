using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AgvError_AddFilteredIndexOnErrorResolved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AgvError_ErrorResolved",
                table: "AgvError",
                column: "ErrorResolved",
                filter: "[ErrorResolved] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgvError_ErrorResolved",
                table: "AgvError");
        }
    }
}
