using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TblPoiCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUserId = table.Column<int>(type: "integer", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblPoiCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TblPoiCategories_TblPoiCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TblPoiCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TblPoiCategories_Users_CreatedUserId",
                        column: x => x.CreatedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TblPoiCategories_Users_ModifiedUserId",
                        column: x => x.ModifiedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TblPois",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    WorkingHours = table.Column<string>(type: "text", nullable: false),
                    Geometry = table.Column<Point>(type: "geometry(Point,4326)", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblPois", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TblPois_TblPoiCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TblPoiCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TblPois_Users_ModifiedUserId",
                        column: x => x.ModifiedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TblPois_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TblPoiCategories_CreatedUserId",
                table: "TblPoiCategories",
                column: "CreatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TblPoiCategories_ModifiedUserId",
                table: "TblPoiCategories",
                column: "ModifiedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TblPoiCategories_ParentId_Name",
                table: "TblPoiCategories",
                columns: new[] { "ParentId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_TblPois_CategoryId",
                table: "TblPois",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TblPois_ModifiedUserId",
                table: "TblPois",
                column: "ModifiedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TblPois_UserId",
                table: "TblPois",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TblPois");

            migrationBuilder.DropTable(
                name: "TblPoiCategories");

        }
    }
}
