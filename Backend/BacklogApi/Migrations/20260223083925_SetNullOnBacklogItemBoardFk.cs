using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BacklogApi.Migrations
{
    /// <inheritdoc />
    public partial class SetNullOnBacklogItemBoardFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BacklogItems_Boards_BoardId",
                table: "BacklogItems");

            migrationBuilder.AddForeignKey(
                name: "FK_BacklogItems_Boards_BoardId",
                table: "BacklogItems",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BacklogItems_Boards_BoardId",
                table: "BacklogItems");

            migrationBuilder.AddForeignKey(
                name: "FK_BacklogItems_Boards_BoardId",
                table: "BacklogItems",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id");
        }
    }
}
