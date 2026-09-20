using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottIn.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyJoinLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyJoinLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyJoinLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyJoinLinks_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyJoinLinks_Employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyJoinLinks_BranchId",
                table: "CompanyJoinLinks",
                column: "BranchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyJoinLinks_CreatedByEmployeeId",
                table: "CompanyJoinLinks",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyJoinLinks_ExpiresAt",
                table: "CompanyJoinLinks",
                column: "ExpiresAt");

            // Accounts are global. This closes the race between two invitations
            // attempting to register the same CPF at the same time.
            migrationBuilder.CreateIndex(
                name: "IX_Employees_CPF",
                table: "Employees",
                column: "CPF",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_CPF",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "CompanyJoinLinks");
        }
    }
}
