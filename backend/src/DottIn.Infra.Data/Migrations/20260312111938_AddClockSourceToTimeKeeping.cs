using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottIn.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClockSourceToTimeKeeping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "TimeKeepings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Mobile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "TimeKeepings");
        }
    }
}
