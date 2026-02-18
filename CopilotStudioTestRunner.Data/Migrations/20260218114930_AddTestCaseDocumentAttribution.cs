using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CopilotStudioTestRunner.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCaseDocumentAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestCaseDocuments",
                columns: table => new
                {
                    TestCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCaseDocuments", x => new { x.TestCaseId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_TestCaseDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestCaseDocuments_TestCases_TestCaseId",
                        column: x => x.TestCaseId,
                        principalTable: "TestCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestCaseDocuments_DocumentId",
                table: "TestCaseDocuments",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestCaseDocuments");
        }
    }
}
