using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class appealupdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppealType",
                table: "Appeals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomFineAmount",
                table: "Appeals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FineId",
                table: "Appeals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FineResolution",
                table: "Appeals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestoredScore",
                table: "Appeals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_FineId",
                table: "Appeals",
                column: "FineId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appeals_Fines_FineId",
                table: "Appeals",
                column: "FineId",
                principalTable: "Fines",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appeals_Fines_FineId",
                table: "Appeals");

            migrationBuilder.DropIndex(
                name: "IX_Appeals_FineId",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "AppealType",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "CustomFineAmount",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "FineId",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "FineResolution",
                table: "Appeals");

            migrationBuilder.DropColumn(
                name: "RestoredScore",
                table: "Appeals");
        }
    }
}
