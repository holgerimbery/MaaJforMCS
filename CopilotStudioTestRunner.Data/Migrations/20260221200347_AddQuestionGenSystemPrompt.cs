using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CopilotStudioTestRunner.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionGenSystemPrompt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SystemPrompt",
                table: "GlobalQuestionGenerationSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionGenSystemPrompt",
                table: "Agents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemPrompt",
                table: "GlobalQuestionGenerationSettings");

            migrationBuilder.DropColumn(
                name: "QuestionGenSystemPrompt",
                table: "Agents");
        }
    }
}
