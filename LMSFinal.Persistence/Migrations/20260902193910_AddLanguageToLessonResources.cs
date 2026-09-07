using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMSFinal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageToLessonResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LessonResources_LessonId",
                table: "LessonResources");

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "LessonResources",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                // Не "" — пустая строка не соответствует ни одному значению
                // LanguageCode, и чтение существующих строк упало бы при разборе.
                // Все материалы, добавленные до этой миграции, русские.
                defaultValue: "RU");

            migrationBuilder.CreateIndex(
                name: "IX_LessonResources_LessonId_LanguageCode",
                table: "LessonResources",
                columns: new[] { "LessonId", "LanguageCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LessonResources_LessonId_LanguageCode",
                table: "LessonResources");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "LessonResources");

            migrationBuilder.CreateIndex(
                name: "IX_LessonResources_LessonId",
                table: "LessonResources",
                column: "LessonId");
        }
    }
}
