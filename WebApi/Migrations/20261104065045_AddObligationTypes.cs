using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddObligationTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ObligationTypeId",
                table: "OtherObligations",
                type: "VARCHAR(36)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ObligationTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(150)", maxLength: 150, nullable: false),
                    IsDeleted = table.Column<bool>(type: "BOOLEAN", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObligationTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtherObl_TypeId",
                table: "OtherObligations",
                column: "ObligationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OblTypes_IsDeleted",
                table: "ObligationTypes",
                column: "IsDeleted");

            migrationBuilder.AddForeignKey(
                name: "FK_OtherObl_OblTypes",
                table: "OtherObligations",
                column: "ObligationTypeId",
                principalTable: "ObligationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OtherObl_OblTypes",
                table: "OtherObligations");

            migrationBuilder.DropTable(
                name: "ObligationTypes");

            migrationBuilder.DropIndex(
                name: "IX_OtherObl_TypeId",
                table: "OtherObligations");

            migrationBuilder.DropColumn(
                name: "ObligationTypeId",
                table: "OtherObligations");
        }
    }
}
