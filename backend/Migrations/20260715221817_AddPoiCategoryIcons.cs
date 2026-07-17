using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiCategoryIcons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "TblPois",
                type: "text",
                nullable: false,
                defaultValue: "default");

            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "TblPoiCategories",
                type: "text",
                nullable: false,
                defaultValue: "default");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "TblPois");

            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "TblPoiCategories");
        }
    }
}
