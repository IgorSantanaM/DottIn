using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottIn.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueDominioEmployeeCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DominioEmployeeMappings_BranchId_DominioCode",
                table: "DominioEmployeeMappings",
                columns: new[] { "BranchId", "DominioCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DominioEmployeeMappings_BranchId_DominioCode",
                table: "DominioEmployeeMappings");
        }
    }
}
