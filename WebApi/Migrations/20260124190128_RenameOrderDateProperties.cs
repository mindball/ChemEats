using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrderDateProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RegisterDate",
                table: "MealOrders",
                newName: "OrderedAt");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "MealOrders",
                newName: "MenuDate");

            migrationBuilder.CreateIndex(
                name: "IX_MealOrders_IsDeleted",
                table: "MealOrders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MealOrders_MenuDate",
                table: "MealOrders",
                column: "MenuDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MealOrders_IsDeleted",
                table: "MealOrders");

            migrationBuilder.DropIndex(
                name: "IX_MealOrders_MenuDate",
                table: "MealOrders");

            migrationBuilder.RenameColumn(
                name: "OrderedAt",
                table: "MealOrders",
                newName: "RegisterDate");

            migrationBuilder.RenameColumn(
                name: "MenuDate",
                table: "MealOrders",
                newName: "Date");
        }
    }
}
