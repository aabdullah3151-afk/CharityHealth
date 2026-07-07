using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharityHealth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorCapacityColumnsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AppointmentDate",
                schema: "charity",
                table: "MedicalRequests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                schema: "charity",
                table: "MedicalRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRequests_DoctorId",
                schema: "charity",
                table: "MedicalRequests",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRequests_Doctors_DoctorId",
                schema: "charity",
                table: "MedicalRequests",
                column: "DoctorId",
                principalSchema: "charity",
                principalTable: "Doctors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRequests_Doctors_DoctorId",
                schema: "charity",
                table: "MedicalRequests");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRequests_DoctorId",
                schema: "charity",
                table: "MedicalRequests");

            migrationBuilder.DropColumn(
                name: "AppointmentDate",
                schema: "charity",
                table: "MedicalRequests");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                schema: "charity",
                table: "MedicalRequests");
        }
    }
}
