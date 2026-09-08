using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMSFinal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorReviewReply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InstructorRepliedAt",
                table: "CourseReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstructorReply",
                table: "CourseReviews",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstructorRepliedAt",
                table: "CourseReviews");

            migrationBuilder.DropColumn(
                name: "InstructorReply",
                table: "CourseReviews");
        }
    }
}
