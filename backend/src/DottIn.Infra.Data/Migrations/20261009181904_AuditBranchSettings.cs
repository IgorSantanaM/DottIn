using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottIn.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditBranchSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BranchSettingsAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: false),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchSettingsAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchSettingsAudits_BranchId_CreatedAtUtc",
                table: "BranchSettingsAudits",
                columns: new[] { "BranchId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchSettingsAudits");
        }
    }
}
