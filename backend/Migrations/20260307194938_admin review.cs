using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class adminreview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserReviews_LoanId_ReviewerId",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_ItemReviews_LoanId",
                table: "ItemReviews");

            migrationBuilder.AlterColumn<int>(
                name: "LoanId",
                table: "UserReviews",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "LoanId",
                table: "ItemReviews",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_LoanId_ReviewerId",
                table: "UserReviews",
                columns: new[] { "LoanId", "ReviewerId" },
                unique: true,
                filter: "[LoanId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviews_LoanId",
                table: "ItemReviews",
                column: "LoanId",
                unique: true,
                filter: "[LoanId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserReviews_LoanId_ReviewerId",
                table: "UserReviews");

            migrationBuilder.DropIndex(
                name: "IX_ItemReviews_LoanId",
                table: "ItemReviews");

            migrationBuilder.AlterColumn<int>(
                name: "LoanId",
                table: "UserReviews",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LoanId",
                table: "ItemReviews",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReviews_LoanId_ReviewerId",
                table: "UserReviews",
                columns: new[] { "LoanId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviews_LoanId",
                table: "ItemReviews",
                column: "LoanId",
                unique: true);
        }
    }
}
