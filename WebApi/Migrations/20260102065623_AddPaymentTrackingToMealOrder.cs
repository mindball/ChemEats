using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTrackingToMealOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidOn",
                table: "MealOrders",
                type: "TIMESTAMP",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "MealOrders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MealOrders_PaymentStatus",
                table: "MealOrders",
                column: "PaymentStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MealOrders_PaymentStatus",
                table: "MealOrders");

            migrationBuilder.DropColumn(
                name: "PaidOn",
                table: "MealOrders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "MealOrders");
        }
    }
}
