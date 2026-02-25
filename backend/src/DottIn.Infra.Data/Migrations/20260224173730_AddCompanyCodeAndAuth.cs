using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottIn.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCodeAndAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Employees",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinHash",
                table: "Employees",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "Branches",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyCode",
                table: "Branches",
                column: "CompanyCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Branches_CompanyCode",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PinHash",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "Branches");
        }
    }
}
