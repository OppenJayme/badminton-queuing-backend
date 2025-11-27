using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class FixQueueEntryIndexAndSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove older duplicates so the tighter unique index can be applied cleanly
            migrationBuilder.Sql(@"
DELETE qe FROM QueueEntries qe
JOIN QueueEntries newer
  ON qe.QueueId = newer.QueueId
 AND qe.PlayerId = newer.PlayerId
 AND qe.Id < newer.Id;
");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_QueueId_PlayerId_IsActive",
                table: "QueueEntries");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_QueueId_PlayerId",
                table: "QueueEntries",
                columns: new[] { "QueueId", "PlayerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_QueueId_PlayerId",
                table: "QueueEntries");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_QueueId_PlayerId_IsActive",
                table: "QueueEntries",
                columns: new[] { "QueueId", "PlayerId", "IsActive" },
                unique: true);
        }
    }
}
