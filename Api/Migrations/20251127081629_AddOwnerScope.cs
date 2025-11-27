using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Queues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Players",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Queues_OwnerUserId",
                table: "Queues",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_OwnerUserId",
                table: "Players",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Queues_OwnerUserId",
                table: "Queues");

            migrationBuilder.DropIndex(
                name: "IX_Players_OwnerUserId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Queues");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Players");
        }
    }
}
