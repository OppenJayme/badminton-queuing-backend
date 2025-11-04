using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchPlayerAndAdminStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "match_players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    GuestSessionId = table.Column<int>(type: "int", nullable: true),
                    EnqueuedAtSnapshot = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_players_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 4, 7, 15, 38, 63, DateTimeKind.Utc).AddTicks(6565), "$2a$11$JeEH.Whr/i2sGbEyNn0VHO09nmzugA2CDyTNxhIhiGY.aiAt3Ut1a" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 4, 7, 15, 38, 63, DateTimeKind.Utc).AddTicks(8579), "$2a$11$ZN9SMlywqrJoZvYGFYmZ5ePJvIqZGc7VCR30nLVPmCPQurVQdfwXC" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 4, 7, 15, 38, 63, DateTimeKind.Utc).AddTicks(8583), "$2a$11$9zcOkn8u7vuUX33NgbIWl.LtMCIcSYLhDQIeh9Fk0siCtzXzYW8Q2" });

            migrationBuilder.CreateIndex(
                name: "IX_match_players_MatchId",
                table: "match_players",
                column: "MatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_players");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 3, 8, 46, 40, 114, DateTimeKind.Utc).AddTicks(7143), "$2a$11$NPS9SvmK1leaTPaMLDMIgO9oaZc657d2WrYG8g.60saRMRFdW.3.G" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 3, 8, 46, 40, 114, DateTimeKind.Utc).AddTicks(9992), "$2a$11$UpEcqZ9AWJKcTBRVSy4yf.uVrwm3TV76mIFfA9lgufUNdZjF9yASK" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 3, 8, 46, 40, 114, DateTimeKind.Utc).AddTicks(9998), "$2a$11$J251amSKSvMFOuUBZIGsNO7DTUrSWOoPkyBwp8viUUX9Yb//kpte." });
        }
    }
}
