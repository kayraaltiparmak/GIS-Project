using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminRolePermissionInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TblPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblUserPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PermissionId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblUserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TblUserPermissions_TblPermissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "TblPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TblUserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TblRolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    PermissionId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblRolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TblRolePermissions_TblPermissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "TblPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TblRolePermissions_TblRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "TblRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TblPermissions",
                columns: new[] { "Id", "Code", "CreatedDate", "Description", "IsActive", "IsDeleted", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, "Point.Create", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Point ekleme yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Point ekleme yetkisi" },
                    { 2, "Point.Update", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Point güncelleme yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Point güncelleme yetkisi" },
                    { 3, "Point.Delete", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Point silme yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Point silme yetkisi" },
                    { 4, "Line.Create", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Line ekleme yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Line ekleme yetkisi" },
                    { 5, "Line.Update", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Line güncelleme yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Line güncelleme yetkisi" },
                    { 6, "Line.Delete", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Line silme yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Line silme yetkisi" },
                    { 7, "Polygon.Create", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Polygon ekleme yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Polygon ekleme yetkisi" },
                    { 8, "Polygon.Update", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Polygon güncelleme yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Polygon güncelleme yetkisi" },
                    { 9, "Polygon.Delete", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Polygon silme yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Polygon silme yetkisi" },
                    { 10, "Inventory.Analyze", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Envanter analizi yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Envanter analizi yetkisi" },
                    { 11, "User.Manage", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Kullanıcı yönetimi yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Kullanıcı yönetimi yetkisi" },
                    { 12, "Role.Manage", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Rol yönetimi yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Rol yönetimi yetkisi" },
                    { 13, "Permission.Manage", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Yetki yönetimi yetkisi", true, false, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Yetki yönetimi yetkisi" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TblPermissions_Code",
                table: "TblPermissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblRolePermissions_PermissionId",
                table: "TblRolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_TblRolePermissions_RoleId_PermissionId",
                table: "TblRolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblRoles_Name",
                table: "TblRoles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblUserPermissions_PermissionId",
                table: "TblUserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_TblUserPermissions_UserId_PermissionId",
                table: "TblUserPermissions",
                columns: new[] { "UserId", "PermissionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_TblRoles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "TblRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_TblRoles_RoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "TblRolePermissions");

            migrationBuilder.DropTable(
                name: "TblUserPermissions");

            migrationBuilder.DropTable(
                name: "TblRoles");

            migrationBuilder.DropTable(
                name: "TblPermissions");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users");
        }
    }
}
