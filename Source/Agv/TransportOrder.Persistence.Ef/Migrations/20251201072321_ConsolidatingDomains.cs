#pragma warning disable 1591, 0472
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidatingDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgvGraphNodes_AgvGraph_AgvGraphsCorrelationId",
                table: "AgvGraphNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_MapNodes_Map_MapsCorrelationId",
                table: "MapNodes");

            migrationBuilder.DropColumn(
                name: "Node_Name",
                table: "TriggerLocations");

            migrationBuilder.DropColumn(
                name: "Target_Node_Name",
                table: "AgvAction");

            migrationBuilder.RenameColumn(
                name: "Node_Id",
                table: "TriggerLocations",
                newName: "NodeId");

            migrationBuilder.RenameColumn(
                name: "MapsCorrelationId",
                table: "MapNodes",
                newName: "MapCorrelationId");

            migrationBuilder.RenameColumn(
                name: "AgvGraphsCorrelationId",
                table: "AgvGraphNodes",
                newName: "EfAgvGraphCorrelationId");

            migrationBuilder.RenameColumn(
                name: "Target_Node_Id",
                table: "AgvAction",
                newName: "Target_NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_TriggerLocations_NodeId",
                table: "TriggerLocations",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_AgvAction_Target_NodeId",
                table: "AgvAction",
                column: "Target_NodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgvAction_GraphNode_Target_NodeId",
                table: "AgvAction",
                column: "Target_NodeId",
                principalTable: "GraphNode",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AgvGraphNodes_AgvGraph_EfAgvGraphCorrelationId",
                table: "AgvGraphNodes",
                column: "EfAgvGraphCorrelationId",
                principalTable: "AgvGraph",
                principalColumn: "CorrelationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MapNodes_Map_MapCorrelationId",
                table: "MapNodes",
                column: "MapCorrelationId",
                principalTable: "Map",
                principalColumn: "CorrelationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TriggerLocations_GraphNode_NodeId",
                table: "TriggerLocations",
                column: "NodeId",
                principalTable: "GraphNode",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgvAction_GraphNode_Target_NodeId",
                table: "AgvAction");

            migrationBuilder.DropForeignKey(
                name: "FK_AgvGraphNodes_AgvGraph_EfAgvGraphCorrelationId",
                table: "AgvGraphNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_MapNodes_Map_MapCorrelationId",
                table: "MapNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_TriggerLocations_GraphNode_NodeId",
                table: "TriggerLocations");

            migrationBuilder.DropIndex(
                name: "IX_TriggerLocations_NodeId",
                table: "TriggerLocations");

            migrationBuilder.DropIndex(
                name: "IX_AgvAction_Target_NodeId",
                table: "AgvAction");

            migrationBuilder.RenameColumn(
                name: "NodeId",
                table: "TriggerLocations",
                newName: "Node_Id");

            migrationBuilder.RenameColumn(
                name: "MapCorrelationId",
                table: "MapNodes",
                newName: "MapsCorrelationId");

            migrationBuilder.RenameColumn(
                name: "EfAgvGraphCorrelationId",
                table: "AgvGraphNodes",
                newName: "AgvGraphsCorrelationId");

            migrationBuilder.RenameColumn(
                name: "Target_NodeId",
                table: "AgvAction",
                newName: "Target_Node_Id");

            migrationBuilder.AddColumn<string>(
                name: "Node_Name",
                table: "TriggerLocations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Target_Node_Name",
                table: "AgvAction",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AgvGraphNodes_AgvGraph_AgvGraphsCorrelationId",
                table: "AgvGraphNodes",
                column: "AgvGraphsCorrelationId",
                principalTable: "AgvGraph",
                principalColumn: "CorrelationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MapNodes_Map_MapsCorrelationId",
                table: "MapNodes",
                column: "MapsCorrelationId",
                principalTable: "Map",
                principalColumn: "CorrelationId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
#pragma warning restore 1591, 0472
