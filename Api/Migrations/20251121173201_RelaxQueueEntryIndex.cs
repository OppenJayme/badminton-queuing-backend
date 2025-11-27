using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class RelaxQueueEntryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FKs to allow index change
            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Players_PlayerId",
                table: "QueueEntries");
            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Queues_QueueId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_QueueId_PlayerId",
                table: "QueueEntries");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 21, 17, 31, 57, 897, DateTimeKind.Utc).AddTicks(3423), "$2a$11$vDD1rC1DcUjQCyxRVjVHg.lcdAf22V402D1f4nlsGn1l0LkBwrQ/a" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 21, 17, 31, 57, 897, DateTimeKind.Utc).AddTicks(6648), "$2a$11$XTFE3C2SCF62mrQjcA7SMOyrjujVxz9cNbIWbPyiP0rom44bib8wm" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 21, 17, 31, 57, 897, DateTimeKind.Utc).AddTicks(6655), "$2a$11$8EgMvAL828k4cyBm9rVZPOM2o9qL4s8onIJOP1ZGlzOtJfsykBcUS" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_QueueId_PlayerId_IsActive",
                table: "QueueEntries",
                columns: new[] { "QueueId", "PlayerId", "IsActive" },
                unique: true);

            // Re-add FKs
            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Players_PlayerId",
                table: "QueueEntries",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Queues_QueueId",
                table: "QueueEntries",
                column: "QueueId",
                principalTable: "Queues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Players_PlayerId",
                table: "QueueEntries");
            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Queues_QueueId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_QueueId_PlayerId_IsActive",
                table: "QueueEntries");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 21, 15, 50, 6, 434, DateTimeKind.Utc).AddTicks(7619), "$2a$11$zvQ7.WHXx8ebOQnAbBKHfu1opoAzrXrFIcTAR/PGq7xHOstpsCfiq" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 21, 15, 50, 6, 434, DateTimeKind.Utc).AddTicks(8973), "$2a$11$fXRaeZ7iIM52KrNF.qxhJeIZ3gWn3S3KyXv/iuahA.IFtBCNFaBdG" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 21, 15, 50, 6, 434, DateTimeKind.Utc).AddTicks(8976), "$2a$11$73PSAlx.FD5XNhAWytYyv.5I0X8GRFpNWOUQnh.mNm0CrPlRNMAQK" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_QueueId_PlayerId",
                table: "QueueEntries",
                columns: new[] { "QueueId", "PlayerId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Players_PlayerId",
                table: "QueueEntries",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Queues_QueueId",
                table: "QueueEntries",
                column: "QueueId",
                principalTable: "Queues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
