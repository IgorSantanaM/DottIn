using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottIn.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeEntryGeolocationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AccuracyMeters",
                table: "TimeEntries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CapturedAtUtc",
                table: "TimeEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "TimeEntries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "TimeEntries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "TimeEntries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Mobile");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimeEntries_Accuracy",
                table: "TimeEntries",
                sql: "\"AccuracyMeters\" IS NULL OR (\"AccuracyMeters\" > 0 AND \"AccuracyMeters\" <= 100)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimeEntries_Location",
                table: "TimeEntries",
                sql: "(\"Latitude\" IS NULL AND \"Longitude\" IS NULL AND \"AccuracyMeters\" IS NULL AND \"CapturedAtUtc\" IS NULL) OR (\"Latitude\" IS NOT NULL AND \"Longitude\" IS NOT NULL AND \"AccuracyMeters\" IS NOT NULL AND \"CapturedAtUtc\" IS NOT NULL AND \"Latitude\" BETWEEN -90 AND 90 AND \"Longitude\" BETWEEN -180 AND 180)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TimeEntries_Source",
                table: "TimeEntries",
                sql: "\"Source\" IN ('Mobile', 'Web', 'Kiosk')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TimeEntries_Accuracy",
                table: "TimeEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimeEntries_Location",
                table: "TimeEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TimeEntries_Source",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "AccuracyMeters",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "CapturedAtUtc",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "TimeEntries");
        }
    }
}
