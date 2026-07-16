using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharityHealth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerProviderProfilesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressAr",
                schema: "charity",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "charity",
                table: "Users",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonName",
                schema: "charity",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyRequestCapacity",
                schema: "charity",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                schema: "charity",
                table: "Users",
                type: "character varying(1500)",
                maxLength: 1500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Governorate",
                schema: "charity",
                table: "Users",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseNumber",
                schema: "charity",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingHours",
                schema: "charity",
                table: "Users",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressAr",
                schema: "charity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "charity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContactPersonName",
                schema: "charity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DailyRequestCapacity",
                schema: "charity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                schema: "charity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Governorate",
                schema: "charity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LicenseNumber",
                schema: "charity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkingHours",
                schema: "charity",
                table: "Users");
        }
    }
}
