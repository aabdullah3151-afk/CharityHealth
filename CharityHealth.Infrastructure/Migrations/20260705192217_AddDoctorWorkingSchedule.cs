using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharityHealth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorWorkingSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "WorkEndTime",
                schema: "charity",
                table: "Doctors",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "WorkStartTime",
                schema: "charity",
                table: "Doctors",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingDays",
                schema: "charity",
                table: "Doctors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkEndTime",
                schema: "charity",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "WorkStartTime",
                schema: "charity",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "WorkingDays",
                schema: "charity",
                table: "Doctors");
        }
    }
}
