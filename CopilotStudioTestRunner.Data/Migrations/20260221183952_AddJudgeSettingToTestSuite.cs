using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CopilotStudioTestRunner.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJudgeSettingToTestSuite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "JudgeSettingId",
                table: "TestSuites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestSuites_JudgeSettingId",
                table: "TestSuites",
                column: "JudgeSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestSuites_JudgeSettings_JudgeSettingId",
                table: "TestSuites",
                column: "JudgeSettingId",
                principalTable: "JudgeSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestSuites_JudgeSettings_JudgeSettingId",
                table: "TestSuites");

            migrationBuilder.DropIndex(
                name: "IX_TestSuites_JudgeSettingId",
                table: "TestSuites");

            migrationBuilder.DropColumn(
                name: "JudgeSettingId",
                table: "TestSuites");
        }
    }
}
