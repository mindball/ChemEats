using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationEmail",
                table: "AspNetUsers",
                type: "VARCHAR(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OrderRemindersEnabled",
                table: "AspNetUsers",
                type: "BOOLEAN",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MenuReminderLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    UserId = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    MenuId = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TIMESTAMP", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuRemLogs_Menus",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuRemLogs_Users",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuRemLogs_MenuId",
                table: "MenuReminderLogs",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "UX_MenuRemLogs_User_Menu",
                table: "MenuReminderLogs",
                columns: new[] { "UserId", "MenuId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuReminderLogs");

            migrationBuilder.DropColumn(
                name: "NotificationEmail",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OrderRemindersEnabled",
                table: "AspNetUsers");
        }
    }
}
