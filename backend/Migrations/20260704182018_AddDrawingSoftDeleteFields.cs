using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDrawingSoftDeleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TblPoints",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TblPoints",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "TblPoints",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TblLines",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TblLines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "TblLines",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TblPolygons",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TblPolygons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "TblPolygons",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TblPoints");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TblPoints");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "TblPoints");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TblLines");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TblLines");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "TblLines");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TblPolygons");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TblPolygons");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "TblPolygons");
        }
    }
}
