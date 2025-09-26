using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceInputAssistant.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdaptiveLearningTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfidenceScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TranscriptionEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    OverallConfidence = table.Column<double>(type: "REAL", nullable: false),
                    WordConfidences = table.Column<string>(type: "TEXT", nullable: false),
                    UncertainWords = table.Column<string>(type: "TEXT", nullable: false),
                    Engine = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ApplicationContext = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConfidenceThreshold = table.Column<double>(type: "REAL", nullable: false),
                    FeedbackRequested = table.Column<bool>(type: "INTEGER", nullable: false),
                    FeedbackReceived = table.Column<bool>(type: "INTEGER", nullable: false),
                    AnalysisMetadata = table.Column<string>(type: "TEXT", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfidenceScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfidenceScores_TranscriptionEvents_TranscriptionEventId",
                        column: x => x.TranscriptionEventId,
                        principalTable: "TranscriptionEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfidenceScores_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ContextPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrecedingContext = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FollowingContext = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ExpectedText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ApplicationContext = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ObservationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceLevel = table.Column<double>(type: "REAL", nullable: false),
                    PatternType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextPatterns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContextPatterns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpeechProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PreferredEngine = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ApplicationContexts = table.Column<string>(type: "TEXT", nullable: false),
                    EngineSettings = table.Column<string>(type: "TEXT", nullable: true),
                    AccuracyMetrics = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeechProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpeechProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TranscriptionEventId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OriginalText = table.Column<string>(type: "TEXT", nullable: false),
                    CorrectedText = table.Column<string>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: true),
                    Engine = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: true),
                    ApplicationContext = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    FeedbackType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsProcessed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFeedbacks_TranscriptionEvents_TranscriptionEventId",
                        column: x => x.TranscriptionEventId,
                        principalTable: "TranscriptionEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserVocabularies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalWord = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CorrectedWord = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceLevel = table.Column<double>(type: "REAL", nullable: false),
                    ApplicationContext = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVocabularies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserVocabularies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfidenceScores_Engine_OverallConfidence",
                table: "ConfidenceScores",
                columns: new[] { "Engine", "OverallConfidence" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfidenceScores_FeedbackRequested",
                table: "ConfidenceScores",
                column: "FeedbackRequested");

            migrationBuilder.CreateIndex(
                name: "IX_ConfidenceScores_TranscriptionEventId_AnalyzedAt",
                table: "ConfidenceScores",
                columns: new[] { "TranscriptionEventId", "AnalyzedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfidenceScores_UserId",
                table: "ConfidenceScores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContextPatterns_IsActive",
                table: "ContextPatterns",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ContextPatterns_UserId_ApplicationContext",
                table: "ContextPatterns",
                columns: new[] { "UserId", "ApplicationContext" });

            migrationBuilder.CreateIndex(
                name: "IX_ContextPatterns_UserId_PrecedingContext_FollowingContext",
                table: "ContextPatterns",
                columns: new[] { "UserId", "PrecedingContext", "FollowingContext" });

            migrationBuilder.CreateIndex(
                name: "IX_SpeechProfiles_IsActive",
                table: "SpeechProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SpeechProfiles_UserId_IsDefault",
                table: "SpeechProfiles",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_SpeechProfiles_UserId_ProfileName",
                table: "SpeechProfiles",
                columns: new[] { "UserId", "ProfileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_Engine_CreatedAt",
                table: "UserFeedbacks",
                columns: new[] { "Engine", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_IsProcessed",
                table: "UserFeedbacks",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_TranscriptionEventId",
                table: "UserFeedbacks",
                column: "TranscriptionEventId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFeedbacks_UserId_CreatedAt",
                table: "UserFeedbacks",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularies_IsActive",
                table: "UserVocabularies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularies_UserId_ApplicationContext",
                table: "UserVocabularies",
                columns: new[] { "UserId", "ApplicationContext" });

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularies_UserId_OriginalWord",
                table: "UserVocabularies",
                columns: new[] { "UserId", "OriginalWord" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfidenceScores");

            migrationBuilder.DropTable(
                name: "ContextPatterns");

            migrationBuilder.DropTable(
                name: "SpeechProfiles");

            migrationBuilder.DropTable(
                name: "UserFeedbacks");

            migrationBuilder.DropTable(
                name: "UserVocabularies");
        }
    }
}
