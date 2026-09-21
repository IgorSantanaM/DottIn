using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottIn.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeKeepingAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimeKeepingAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeKeepingId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OriginalTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProposedTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReviewNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeKeepingAdjustments", x => x.Id);
                    table.CheckConstraint("CK_TimeKeepingAdjustments_EntryType", "\"EntryType\" IN ('ClockIn', 'BreakStart', 'BreakEnd', 'ClockOut')");
                    table.CheckConstraint("CK_TimeKeepingAdjustments_Status", "\"Status\" IN ('Pending', 'Approved', 'Rejected')");
                    table.ForeignKey(
                        name: "FK_TimeKeepingAdjustments_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeKeepingAdjustments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeKeepingAdjustments_Employees_RequestedByEmployeeId",
                        column: x => x.RequestedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeKeepingAdjustments_Employees_ReviewedByEmployeeId",
                        column: x => x.ReviewedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeKeepingAdjustments_TimeKeepings_TimeKeepingId",
                        column: x => x.TimeKeepingId,
                        principalTable: "TimeKeepings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeKeepingAdjustments_BranchId_Status_CreatedAt",
                table: "TimeKeepingAdjustments",
                columns: new[] { "BranchId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeKeepingAdjustments_EmployeeId_CreatedAt",
                table: "TimeKeepingAdjustments",
                columns: new[] { "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeKeepingAdjustments_RequestedByEmployeeId",
                table: "TimeKeepingAdjustments",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeKeepingAdjustments_ReviewedByEmployeeId",
                table: "TimeKeepingAdjustments",
                column: "ReviewedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeKeepingAdjustments_TimeKeepingId",
                table: "TimeKeepingAdjustments",
                column: "TimeKeepingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimeKeepingAdjustments");
        }
    }
}
