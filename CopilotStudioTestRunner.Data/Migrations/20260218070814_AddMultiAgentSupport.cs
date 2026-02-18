using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CopilotStudioTestRunner.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiAgentSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    OldValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DocumentType = table.Column<string>(type: "TEXT", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JudgeSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PromptTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TaskSuccessWeight = table.Column<double>(type: "REAL", nullable: false),
                    IntentMatchWeight = table.Column<double>(type: "REAL", nullable: false),
                    FactualityWeight = table.Column<double>(type: "REAL", nullable: false),
                    HelpfulnessWeight = table.Column<double>(type: "REAL", nullable: false),
                    SafetyWeight = table.Column<double>(type: "REAL", nullable: false),
                    PassThreshold = table.Column<double>(type: "REAL", nullable: false),
                    UseReferenceAnswer = table.Column<bool>(type: "INTEGER", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Temperature = table.Column<double>(type: "REAL", nullable: false),
                    TopP = table.Column<double>(type: "REAL", nullable: false),
                    MaxOutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JudgeSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestSuites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultTimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxRetries = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestSuites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Environment = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    DirectLineBotId = table.Column<string>(type: "TEXT", nullable: false),
                    DirectLineSecret = table.Column<string>(type: "TEXT", nullable: false),
                    DirectLineUseWebChannelSecret = table.Column<bool>(type: "INTEGER", nullable: false),
                    DirectLineUseWebSocket = table.Column<bool>(type: "INTEGER", nullable: false),
                    DirectLineReplyTimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    DirectLineMaxRetries = table.Column<int>(type: "INTEGER", nullable: false),
                    DirectLineBackoffSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    JudgeEndpoint = table.Column<string>(type: "TEXT", nullable: false),
                    JudgeApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    JudgeModel = table.Column<string>(type: "TEXT", nullable: false),
                    JudgeTemperature = table.Column<double>(type: "REAL", nullable: false),
                    JudgeTopP = table.Column<double>(type: "REAL", nullable: false),
                    JudgePassThreshold = table.Column<double>(type: "REAL", nullable: false),
                    JudgeMaxOutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionGenEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    QuestionGenApiKey = table.Column<string>(type: "TEXT", nullable: true),
                    QuestionGenModel = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalQuestionGenerationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Temperature = table.Column<double>(type: "REAL", nullable: false),
                    TopP = table.Column<double>(type: "REAL", nullable: false),
                    MaxOutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalQuestionGenerationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Chunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    TokenCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ChunkIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    StartChapter = table.Column<double>(type: "REAL", nullable: true),
                    EndChapter = table.Column<double>(type: "REAL", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Embedding = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chunks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SuiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutionUser = table.Column<string>(type: "TEXT", nullable: true),
                    GitSha = table.Column<string>(type: "TEXT", nullable: true),
                    ModelVersion = table.Column<string>(type: "TEXT", nullable: true),
                    PromptVersion = table.Column<string>(type: "TEXT", nullable: true),
                    TotalTestCases = table.Column<int>(type: "INTEGER", nullable: false),
                    PassedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageLatencyMs = table.Column<double>(type: "REAL", nullable: false),
                    MedianLatencyMs = table.Column<double>(type: "REAL", nullable: false),
                    P95LatencyMs = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Runs_TestSuites_SuiteId",
                        column: x => x.SuiteId,
                        principalTable: "TestSuites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Runs_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TestCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SuiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    UserInput = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectedIntent = table.Column<string>(type: "TEXT", nullable: true),
                    ExpectedEntities = table.Column<string>(type: "TEXT", nullable: false),
                    AcceptanceCriteria = table.Column<string>(type: "TEXT", nullable: false),
                    ReferenceAnswer = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsGenerated = table.Column<bool>(type: "INTEGER", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxRetries = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCases_Documents_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TestCases_TestSuites_SuiteId",
                        column: x => x.SuiteId,
                        principalTable: "TestSuites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestCaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Verdict = table.Column<string>(type: "TEXT", nullable: false),
                    TaskSuccessScore = table.Column<double>(type: "REAL", nullable: true),
                    IntentMatchScore = table.Column<double>(type: "REAL", nullable: true),
                    FactualityScore = table.Column<double>(type: "REAL", nullable: true),
                    HelpfulnessScore = table.Column<double>(type: "REAL", nullable: true),
                    SafetyScore = table.Column<double>(type: "REAL", nullable: true),
                    OverallScore = table.Column<double>(type: "REAL", nullable: true),
                    LatencyMs = table.Column<long>(type: "INTEGER", nullable: false),
                    TurnCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TokensUsed = table.Column<int>(type: "INTEGER", nullable: true),
                    JudgeRationale = table.Column<string>(type: "TEXT", nullable: true),
                    JudgeCitations = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Results_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Results_TestCases_TestCaseId",
                        column: x => x.TestCaseId,
                        principalTable: "TestCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Results_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ResultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RawActivityJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptMessages_Results_ResultId",
                        column: x => x.ResultId,
                        principalTable: "Results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestSuiteAgents",
                columns: table => new
                {
                    TestSuiteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestSuiteAgents", x => new { x.TestSuiteId, x.AgentId });
                    table.ForeignKey(
                        name: "FK_TestSuiteAgents_TestSuites_TestSuiteId",
                        column: x => x.TestSuiteId,
                        principalTable: "TestSuites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestSuiteAgents_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_DocumentId",
                table: "Chunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TestSuiteAgents_AgentId",
                table: "TestSuiteAgents",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_AgentId",
                table: "Runs",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_RunId_TestCaseId",
                table: "Results",
                columns: new[] { "RunId", "TestCaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Results_AgentId",
                table: "Results",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_TestCaseId",
                table: "Results",
                column: "TestCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_SuiteId",
                table: "Runs",
                column: "SuiteId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_SourceDocumentId",
                table: "TestCases",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_SuiteId",
                table: "TestCases",
                column: "SuiteId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptMessages_ResultId",
                table: "TranscriptMessages",
                column: "ResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Chunks");

            migrationBuilder.DropTable(
                name: "GlobalQuestionGenerationSettings");

            migrationBuilder.DropTable(
                name: "JudgeSettings");

            migrationBuilder.DropTable(
                name: "TestSuiteAgents");

            migrationBuilder.DropTable(
                name: "TranscriptMessages");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Runs");

            migrationBuilder.DropTable(
                name: "TestCases");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "TestSuites");
        }
    }
}
