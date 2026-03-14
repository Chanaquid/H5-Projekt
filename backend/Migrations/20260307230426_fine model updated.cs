using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class finemodelupdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appeals_Fines_FineId",
                table: "Appeals");

            migrationBuilder.DropIndex(
                name: "IX_Fines_UserId_IsPaid",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Fines");

            migrationBuilder.AddColumn<string>(
                name: "PaymentDescription",
                table: "Fines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentProofImageUrl",
                table: "Fines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Fines",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Fines",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Fines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FineResolution",
                table: "Appeals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomFineAmount",
                table: "Appeals",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AppealType",
                table: "Appeals",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_UserId_Status",
                table: "Fines",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Appeals_Fines_FineId",
                table: "Appeals",
                column: "FineId",
                principalTable: "Fines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appeals_Fines_FineId",
                table: "Appeals");

            migrationBuilder.DropIndex(
                name: "IX_Fines_UserId_Status",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "PaymentDescription",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "PaymentProofImageUrl",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Fines");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Fines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Fines",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FineResolution",
                table: "Appeals",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomFineAmount",
                table: "Appeals",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AppealType",
                table: "Appeals",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_UserId_IsPaid",
                table: "Fines",
                columns: new[] { "UserId", "IsPaid" });

            migrationBuilder.AddForeignKey(
                name: "FK_Appeals_Fines_FineId",
                table: "Appeals",
                column: "FineId",
                principalTable: "Fines",
                principalColumn: "Id");
        }
    }
}
