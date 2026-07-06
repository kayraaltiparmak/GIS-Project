using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDrawingUserOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<int>(
                name: "ModifiedUserId",
                table: "TblPolygons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "TblPolygons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedUserId",
                table: "TblPoints",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "TblPoints",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedUserId",
                table: "TblLines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "TblLines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TblPolygons_ModifiedUserId",
                table: "TblPolygons",
                column: "ModifiedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TblPolygons_UserId",
                table: "TblPolygons",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TblPoints_ModifiedUserId",
                table: "TblPoints",
                column: "ModifiedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TblPoints_UserId",
                table: "TblPoints",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TblLines_ModifiedUserId",
                table: "TblLines",
                column: "ModifiedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TblLines_UserId",
                table: "TblLines",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TblLines_Users_ModifiedUserId",
                table: "TblLines",
                column: "ModifiedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TblLines_Users_UserId",
                table: "TblLines",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TblPoints_Users_ModifiedUserId",
                table: "TblPoints",
                column: "ModifiedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TblPoints_Users_UserId",
                table: "TblPoints",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TblPolygons_Users_ModifiedUserId",
                table: "TblPolygons",
                column: "ModifiedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TblPolygons_Users_UserId",
                table: "TblPolygons",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TblLines_Users_ModifiedUserId",
                table: "TblLines");

            migrationBuilder.DropForeignKey(
                name: "FK_TblLines_Users_UserId",
                table: "TblLines");

            migrationBuilder.DropForeignKey(
                name: "FK_TblPoints_Users_ModifiedUserId",
                table: "TblPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_TblPoints_Users_UserId",
                table: "TblPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_TblPolygons_Users_ModifiedUserId",
                table: "TblPolygons");

            migrationBuilder.DropForeignKey(
                name: "FK_TblPolygons_Users_UserId",
                table: "TblPolygons");

            migrationBuilder.DropIndex(
                name: "IX_TblPolygons_ModifiedUserId",
                table: "TblPolygons");

            migrationBuilder.DropIndex(
                name: "IX_TblPolygons_UserId",
                table: "TblPolygons");

            migrationBuilder.DropIndex(
                name: "IX_TblPoints_ModifiedUserId",
                table: "TblPoints");

            migrationBuilder.DropIndex(
                name: "IX_TblPoints_UserId",
                table: "TblPoints");

            migrationBuilder.DropIndex(
                name: "IX_TblLines_ModifiedUserId",
                table: "TblLines");

            migrationBuilder.DropIndex(
                name: "IX_TblLines_UserId",
                table: "TblLines");

            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "TblPolygons");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TblPolygons");

            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "TblPoints");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TblPoints");

            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "TblLines");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TblLines");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
