using System;
using FirebirdSql.EntityFrameworkCore.Firebird.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    Abbreviation = table.Column<string>(type: "VARCHAR(3)", maxLength: 3, nullable: false),
                    FullName = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: false),
                    UserName = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "VARCHAR(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    PasswordHash = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    LockoutEnd = table.Column<string>(type: "VARCHAR(48)", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Fb:ValueGenerationStrategy", FbValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    ClaimType = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRole~",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Fb:ValueGenerationStrategy", FbValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    ClaimType = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUser~",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    ProviderKey = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    UserId = table.Column<string>(type: "VARCHAR(256)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUser~",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    RoleId = table.Column<string>(type: "VARCHAR(256)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles~",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers~",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    LoginProvider = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    Value = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUser~",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    VatNumber = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: false),
                    PaymentTerms = table.Column<int>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    Phone = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    StreetAddress = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    City = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    PostalCode = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    Country = table.Column<string>(type: "BLOB SUB_TYPE TEXT", nullable: true),
                    SupervisorId = table.Column<string>(type: "VARCHAR(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_AspNetUsers_Super~",
                        column: x => x.SupervisorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    SupplierId = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    Date = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    ActiveUntil = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    RegisterDate = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    IsDeleted = table.Column<bool>(type: "BOOLEAN", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menus_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Meals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    MenuId = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    Price_Amount = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meals_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MealOrders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    UserId = table.Column<string>(type: "VARCHAR(256)", nullable: false),
                    MealId = table.Column<string>(type: "VARCHAR(36)", nullable: false),
                    MenuDate = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    OrderedAt = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentStatus = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    PaidOn = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    IsDeleted = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    PortionApplied = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    PortionAmount = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealOrders_AspNetUsers_User~",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealOrders_Meals_MealId",
                        column: x => x.MealId,
                        principalTable: "Meals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealOrders_IsDeleted",
                table: "MealOrders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MealOrders_MealId",
                table: "MealOrders",
                column: "MealId");

            migrationBuilder.CreateIndex(
                name: "IX_MealOrders_MenuDate",
                table: "MealOrders",
                column: "MenuDate");

            migrationBuilder.CreateIndex(
                name: "IX_MealOrders_PaymentStatus",
                table: "MealOrders",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MealOrders_UserId",
                table: "MealOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meals_MenuId",
                table: "Meals",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Date",
                table: "Menus",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_IsDeleted",
                table: "Menus",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_SupplierId_Date",
                table: "Menus",
                columns: new[] { "SupplierId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupervisorId",
                table: "Suppliers",
                column: "SupervisorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "MealOrders");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Meals");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
