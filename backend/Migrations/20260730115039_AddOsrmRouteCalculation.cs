using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOsrmRouteCalculation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DistanceMeters",
                table: "TblTransportRoutes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DurationSeconds",
                table: "TblTransportRoutes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<LineString>(
                name: "Geometry",
                table: "TblTransportRoutes",
                type: "geometry(LineString,4326)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RouteCalculatedAt",
                table: "TblTransportRoutes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteCalculationError",
                table: "TblTransportRoutes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteCalculationStatus",
                table: "TblTransportRoutes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblTransportRoutes_Geometry",
                table: "TblTransportRoutes",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TblTransportRoutes_Geometry",
                table: "TblTransportRoutes");

            migrationBuilder.DropColumn(
                name: "DistanceMeters",
                table: "TblTransportRoutes");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "TblTransportRoutes");

            migrationBuilder.DropColumn(
                name: "Geometry",
                table: "TblTransportRoutes");

            migrationBuilder.DropColumn(
                name: "RouteCalculatedAt",
                table: "TblTransportRoutes");

            migrationBuilder.DropColumn(
                name: "RouteCalculationError",
                table: "TblTransportRoutes");

            migrationBuilder.DropColumn(
                name: "RouteCalculationStatus",
                table: "TblTransportRoutes");
        }
    }
}
