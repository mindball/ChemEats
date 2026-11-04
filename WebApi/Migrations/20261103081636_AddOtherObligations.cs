using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOtherObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OtherObligations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    UserId = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    Description = table.Column<string>(type: "VARCHAR(300)", maxLength: 300, nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "VARCHAR(450)", maxLength: 450, nullable: false),
                    PaymentStatus = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    PaidOn = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    PaidByUserId = table.Column<string>(type: "VARCHAR(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "BOOLEAN", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherObligations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherObl_Users",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtherObl_Date",
                table: "OtherObligations",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_OtherObl_PaymentStatus",
                table: "OtherObligations",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_OtherObl_UserId",
                table: "OtherObligations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtherObligations");
        }
    }
}
