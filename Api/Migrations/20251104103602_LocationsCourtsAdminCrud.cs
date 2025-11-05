using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class LocationsCourtsAdminCrud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Locations",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Locations",
                type: "longtext",
                nullable: false,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Courts",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_0900_ai_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Court 1");

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Court 2");

            migrationBuilder.UpdateData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "City", "Name" },
                values: new object[] { "Cebu City", "Cebu City", "Metro Sports" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 4, 10, 35, 58, 785, DateTimeKind.Utc).AddTicks(9380), "$2a$11$nlx9EmftOpjPldt2QQk19uADWK7TMnnmCGGe8UpwVOajciAoi5n7y" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 4, 10, 35, 58, 786, DateTimeKind.Utc).AddTicks(662), "$2a$11$VgSq3/Rbz/xsWDnnx6a8YObx1QyzMe6HT1QrKiEkQ9aRfj.SVCcxe" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2025, 11, 4, 10, 35, 58, 786, DateTimeKind.Utc).AddTicks(666), "$2a$11$3dMcxRnDgdE78FR7mnvClOw3hJtV1jMs4Y4.s6XPy73u0fiseB26u" });

            migrationBuilder.CreateIndex(
                name: "IX_Courts_LocationId_CourtNumber",
                table: "Courts",
                columns: new[] { "LocationId", "CourtNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courts_LocationId_CourtNumber",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Courts");

            migrationBuilder.UpdateData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "SmashPoint Badminton Center");

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
                name: "IX_Courts_LocationId",
                table: "Courts",
                column: "LocationId");
        }
    }
}
