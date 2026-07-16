using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CharityHealth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManualCompletedServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(

                            name: "ManualServiceRecords",

                            schema: "charity",

                            columns: table => new

                            {

                                Id = table.Column<Guid>(type: "uuid", nullable: false),

                                ServiceType = table.Column<int>(type: "integer", nullable: false),

                                ProviderUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),

                                DoctorId = table.Column<Guid>(type: "uuid", nullable: true),

                                ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),

                                Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),

                                Notes = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),

                                CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),

                                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),

                                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),

                                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),

                                CreatedBy = table.Column<string>(type: "text", nullable: true),

                                UpdatedBy = table.Column<string>(type: "text", nullable: true)

                            },

                            constraints: table =>

                            {

                                table.PrimaryKey("PK_ManualServiceRecords", x => x.Id);

                                table.ForeignKey(

                                    name: "FK_ManualServiceRecords_Doctors_DoctorId",

                                    column: x => x.DoctorId,

                                    principalSchema: "charity",

                                    principalTable: "Doctors",

                                    principalColumn: "Id",

                                    onDelete: ReferentialAction.Restrict);

                                table.ForeignKey(

                                    name: "FK_ManualServiceRecords_Users_ProviderUserId",

                                    column: x => x.ProviderUserId,

                                    principalSchema: "charity",

                                    principalTable: "Users",

                                    principalColumn: "Id",

                                    onDelete: ReferentialAction.Restrict);

                            });

            migrationBuilder.CreateIndex(

                            name: "IX_ManualServiceRecords_DoctorId",

                            schema: "charity",

                            table: "ManualServiceRecords",

                            column: "DoctorId");

            migrationBuilder.CreateIndex(

                            name: "IX_ManualServiceRecords_ProviderUserId_ServiceType_ServiceDate",

                            schema: "charity",

                            table: "ManualServiceRecords",

                            columns: new[] { "ProviderUserId", "ServiceType", "ServiceDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualServiceRecords",
                schema: "charity");
        }
    }
}
