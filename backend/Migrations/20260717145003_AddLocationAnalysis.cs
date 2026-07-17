using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocationAnalysisResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PoiId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    CriterionWeight = table.Column<int>(type: "integer", nullable: false),
                    HeatWeight = table.Column<double>(type: "double precision", nullable: false),
                    Geometry = table.Column<Point>(type: "geometry(Point,4326)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationAnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationAnalysisResults_TblPoiCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TblPoiCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocationAnalysisResults_TblPois_PoiId",
                        column: x => x.PoiId,
                        principalTable: "TblPois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocationAnalysisResults_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProvinceBoundaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Geometry = table.Column<Geometry>(type: "geometry(Geometry,4326)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvinceBoundaries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TblPois_Geometry",
                table: "TblPois",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_LocationAnalysisResults_AnalysisId_UserId_IsActive",
                table: "LocationAnalysisResults",
                columns: new[] { "AnalysisId", "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationAnalysisResults_CategoryId",
                table: "LocationAnalysisResults",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationAnalysisResults_Geometry",
                table: "LocationAnalysisResults",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_LocationAnalysisResults_PoiId",
                table: "LocationAnalysisResults",
                column: "PoiId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationAnalysisResults_UserId",
                table: "LocationAnalysisResults",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvinceBoundaries_Geometry",
                table: "ProvinceBoundaries",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_ProvinceBoundaries_Name",
                table: "ProvinceBoundaries",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationAnalysisResults");

            migrationBuilder.DropTable(
                name: "ProvinceBoundaries");

            migrationBuilder.DropIndex(
                name: "IX_TblPois_Geometry",
                table: "TblPois");
        }
    }
}
