using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharityHealth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerDiscountPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                schema: "charity",
                table: "Users",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                schema: "charity",
                table: "Users");
        }
    }
}
