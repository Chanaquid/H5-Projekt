using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class reviewsupdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "UserReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdminReview",
                table: "UserReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "UserReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "ItemReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdminReview",
                table: "ItemReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "ItemReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "UserReviews");

            migrationBuilder.DropColumn(
                name: "IsAdminReview",
                table: "UserReviews");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "UserReviews");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "ItemReviews");

            migrationBuilder.DropColumn(
                name: "IsAdminReview",
                table: "ItemReviews");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "ItemReviews");
        }
    }
}
