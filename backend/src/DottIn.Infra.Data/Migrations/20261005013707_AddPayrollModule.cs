using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottIn.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeInvitations_Employees_BranchId_InvitedByEmployeeId",
                table: "EmployeeInvitations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_Role",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeInvitations_Role",
                table: "EmployeeInvitations");

            migrationBuilder.CreateTable(
                name: "AccountantBranchAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountantEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountantBranchAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountantBranchAccesses_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountantBranchAccesses_Employees_AccountantEmployeeId",
                        column: x => x.AccountantEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payrolls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExportedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payrolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payrolls_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payrolls_Employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollExportEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollExportEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollExportEvents_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DominioCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    WorkedMinutes = table.Column<long>(type: "bigint", nullable: false),
                    HasIncompleteRecords = table.Column<bool>(type: "boolean", nullable: false),
                    CalculatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollItems_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollItems_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPaymentChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NewAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPaymentChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentChanges_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_Role",
                table: "Employees",
                sql: "\"Role\" IN ('Employee', 'Manager', 'Administrator', 'Owner', 'Accountant')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeInvitations_Role",
                table: "EmployeeInvitations",
                sql: "\"Role\" IN ('Employee', 'Manager', 'Administrator', 'Accountant')");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeInvitations_Employees_InvitedByEmployeeId",
                table: "EmployeeInvitations",
                column: "InvitedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateIndex(
                name: "IX_AccountantBranchAccesses_AccountantEmployeeId",
                table: "AccountantBranchAccesses",
                column: "AccountantEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountantBranchAccesses_BranchId_AccountantEmployeeId",
                table: "AccountantBranchAccesses",
                columns: new[] { "BranchId", "AccountantEmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollExportEvents_PayrollId_ExportedAt",
                table: "PayrollExportEvents",
                columns: new[] { "PayrollId", "ExportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItems_EmployeeId",
                table: "PayrollItems",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollItems_PayrollId_EmployeeId",
                table: "PayrollItems",
                columns: new[] { "PayrollId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentChanges_PayrollId_ChangedAt",
                table: "PayrollPaymentChanges",
                columns: new[] { "PayrollId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_BranchId_Year_Month",
                table: "Payrolls",
                columns: new[] { "BranchId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_CreatedByEmployeeId",
                table: "Payrolls",
                column: "CreatedByEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeInvitations_Employees_InvitedByEmployeeId",
                table: "EmployeeInvitations");

            migrationBuilder.DropTable(
                name: "AccountantBranchAccesses");

            migrationBuilder.DropTable(
                name: "PayrollExportEvents");

            migrationBuilder.DropTable(
                name: "PayrollItems");

            migrationBuilder.DropTable(
                name: "PayrollPaymentChanges");

            migrationBuilder.DropTable(
                name: "Payrolls");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_Role",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeInvitations_Role",
                table: "EmployeeInvitations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_Role",
                table: "Employees",
                sql: "\"Role\" IN ('Employee', 'Manager', 'Administrator', 'Owner')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeInvitations_Role",
                table: "EmployeeInvitations",
                sql: "\"Role\" IN ('Employee', 'Manager', 'Administrator')");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeInvitations_Employees_BranchId_InvitedByEmployeeId",
                table: "EmployeeInvitations",
                columns: new[] { "BranchId", "InvitedByEmployeeId" },
                principalTable: "Employees",
                principalColumns: new[] { "BranchId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
