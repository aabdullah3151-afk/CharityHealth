using CharityHealth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharityHealth.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260712164500_AddServiceRequestTypes")]
public partial class AddServiceRequestTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "SpecialtyId",
            schema: "charity",
            table: "MedicalRequests",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<string>(
            name: "AssignedProviderUserId",
            schema: "charity",
            table: "MedicalRequests",
            type: "character varying(450)",
            maxLength: 450,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "FulfilledAt",
            schema: "charity",
            table: "MedicalRequests",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProviderNoteAr",
            schema: "charity",
            table: "MedicalRequests",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ServiceType",
            schema: "charity",
            table: "MedicalRequests",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateIndex(
            name: "IX_MedicalRequests_AssignedProviderUserId",
            schema: "charity",
            table: "MedicalRequests",
            column: "AssignedProviderUserId");

        migrationBuilder.CreateIndex(
            name: "IX_MedicalRequests_ServiceType",
            schema: "charity",
            table: "MedicalRequests",
            column: "ServiceType");

        migrationBuilder.AddForeignKey(
            name: "FK_MedicalRequests_Users_AssignedProviderUserId",
            schema: "charity",
            table: "MedicalRequests",
            column: "AssignedProviderUserId",
            principalSchema: "charity",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MedicalRequests_Users_AssignedProviderUserId",
            schema: "charity",
            table: "MedicalRequests");

        migrationBuilder.DropIndex(
            name: "IX_MedicalRequests_AssignedProviderUserId",
            schema: "charity",
            table: "MedicalRequests");

        migrationBuilder.DropIndex(
            name: "IX_MedicalRequests_ServiceType",
            schema: "charity",
            table: "MedicalRequests");

        migrationBuilder.Sql("DELETE FROM charity.\"MedicalRequests\" WHERE \"SpecialtyId\" IS NULL;");

        migrationBuilder.DropColumn(
            name: "AssignedProviderUserId",
            schema: "charity",
            table: "MedicalRequests");

        migrationBuilder.DropColumn(
            name: "FulfilledAt",
            schema: "charity",
            table: "MedicalRequests");

        migrationBuilder.DropColumn(
            name: "ProviderNoteAr",
            schema: "charity",
            table: "MedicalRequests");

        migrationBuilder.DropColumn(
            name: "ServiceType",
            schema: "charity",
            table: "MedicalRequests");

        migrationBuilder.AlterColumn<Guid>(
            name: "SpecialtyId",
            schema: "charity",
            table: "MedicalRequests",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }
}
