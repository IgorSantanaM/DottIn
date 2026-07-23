using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottIn.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenMultiTenantSecurityAndTimekeeping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "TimeKeepings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "TimeKeepings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Employees",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Employee");

            // Tokens previously persisted in plain text cannot be migrated safely to the
            // new hash-only format. Revoking them forces a new authenticated session.
            migrationBuilder.Sql("DELETE FROM \"RefreshTokens\";");

            migrationBuilder.Sql(
                "UPDATE \"Employees\" AS e " +
                "SET \"Role\" = 'Owner' " +
                "WHERE EXISTS (SELECT 1 FROM \"Branches\" AS b WHERE b.\"OwnerId\" = e.\"Id\");");

            migrationBuilder.Sql(
                "UPDATE \"TimeKeepings\" AS tk " +
                "SET \"TimeZoneId\" = COALESCE(NULLIF(b.\"TimeZoneId\", ''), 'UTC') " +
                "FROM \"Branches\" AS b WHERE b.\"Id\" = tk.\"BranchId\";");

            migrationBuilder.Sql(
                "UPDATE \"TimeKeepings\" SET \"TimeZoneId\" = 'UTC' " +
                "WHERE \"TimeZoneId\" IS NULL OR length(trim(\"TimeZoneId\")) = 0;");

            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                table: "TimeKeepings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "UTC",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Employees_BranchId_Id",
                table: "Employees",
                columns: new[] { "BranchId", "Id" });

            migrationBuilder.CreateTable(
                name: "EmployeeInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeInvitations", x => x.Id);
                    table.CheckConstraint("CK_EmployeeInvitations_Role", "\"Role\" IN ('Employee', 'Manager', 'Administrator')");
                    table.ForeignKey(
                        name: "FK_EmployeeInvitations_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeInvitations_Employees_BranchId_InvitedByEmployeeId",
                        columns: x => new { x.BranchId, x.InvitedByEmployeeId },
                        principalTable: "Employees",
                        principalColumns: new[] { "BranchId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeInvitations_Employees_ConsumedByEmployeeId",
                        column: x => x.ConsumedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StripeWebhookReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeWebhookReceipts", x => x.Id);
                    table.CheckConstraint("CK_StripeWebhookReceipts_Status", "\"Status\" IN ('Processing', 'Processed', 'Failed')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeKeepings_BranchId_EmployeeId",
                table: "TimeKeepings",
                columns: new[] { "BranchId", "EmployeeId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimeKeepings_Source",
                table: "TimeKeepings",
                sql: "\"Source\" IN ('Mobile', 'Web', 'Kiosk')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimeKeepings_TimeZoneId",
                table: "TimeKeepings",
                sql: "length(trim(\"TimeZoneId\")) > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BranchId_Role_IsActive",
                table: "Employees",
                columns: new[] { "BranchId", "Role", "IsActive" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employees_Role",
                table: "Employees",
                sql: "\"Role\" IN ('Employee', 'Manager', 'Administrator', 'Owner')");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_BranchId_ExpiresAt",
                table: "EmployeeInvitations",
                columns: new[] { "BranchId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_BranchId_InvitedByEmployeeId",
                table: "EmployeeInvitations",
                columns: new[] { "BranchId", "InvitedByEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_ConsumedByEmployeeId",
                table: "EmployeeInvitations",
                column: "ConsumedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_InvitedByEmployeeId",
                table: "EmployeeInvitations",
                column: "InvitedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvitations_TokenHash",
                table: "EmployeeInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookReceipts_EventId",
                table: "StripeWebhookReceipts",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookReceipts_EventType",
                table: "StripeWebhookReceipts",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookReceipts_Status",
                table: "StripeWebhookReceipts",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeKeepings_Branches_BranchId",
                table: "TimeKeepings",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeKeepings_Employees_BranchId_EmployeeId",
                table: "TimeKeepings",
                columns: new[] { "BranchId", "EmployeeId" },
                principalTable: "Employees",
                principalColumns: new[] { "BranchId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeKeepings_Branches_BranchId",
                table: "TimeKeepings");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeKeepings_Employees_BranchId_EmployeeId",
                table: "TimeKeepings");

            migrationBuilder.DropTable(
                name: "EmployeeInvitations");

            migrationBuilder.DropTable(
                name: "StripeWebhookReceipts");

            migrationBuilder.DropIndex(
                name: "IX_TimeKeepings_BranchId_EmployeeId",
                table: "TimeKeepings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimeKeepings_Source",
                table: "TimeKeepings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimeKeepings_TimeZoneId",
                table: "TimeKeepings");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Employees_BranchId_Id",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_BranchId_Role_IsActive",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employees_Role",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "TimeKeepings");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "TimeKeepings");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Employees");
        }
    }
}
