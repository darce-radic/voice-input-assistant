using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceInputAssistant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApplicationExecutables = table.Column<string>(type: "TEXT", nullable: false),
                    WindowTitlePatterns = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessNamePatterns = table.Column<string>(type: "TEXT", nullable: false),
                    SpeechRecognitionSettings = table.Column<string>(type: "TEXT", nullable: true),
                    HotkeyConfigs = table.Column<string>(type: "TEXT", nullable: false),
                    TextProcessingRules = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EngineMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EngineName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AverageConfidence = table.Column<float>(type: "REAL", nullable: false),
                    AverageProcessingTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTranscriptions = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessfulTranscriptions = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedTranscriptions = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorRate = table.Column<float>(type: "REAL", nullable: false),
                    AdditionalMetrics = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EngineMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ErrorType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: false),
                    StackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    ApplicationContext = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AdditionalData = table.Column<string>(type: "TEXT", nullable: true),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedText = table.Column<string>(type: "TEXT", nullable: true),
                    ApplicationName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Engine = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceScore = table.Column<float>(type: "REAL", nullable: false),
                    ProcessingTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptionEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EngineMetrics_EngineName_Timestamp",
                table: "EngineMetrics",
                columns: new[] { "EngineName", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_EngineMetrics_Timestamp",
                table: "EngineMetrics",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorEvents_ErrorType_Timestamp",
                table: "ErrorEvents",
                columns: new[] { "ErrorType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorEvents_Timestamp",
                table: "ErrorEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptionEvents_ApplicationName_Timestamp",
                table: "TranscriptionEvents",
                columns: new[] { "ApplicationName", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptionEvents_Timestamp",
                table: "TranscriptionEvents",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationProfiles");

            migrationBuilder.DropTable(
                name: "EngineMetrics");

            migrationBuilder.DropTable(
                name: "ErrorEvents");

            migrationBuilder.DropTable(
                name: "TranscriptionEvents");
        }
    }
}
