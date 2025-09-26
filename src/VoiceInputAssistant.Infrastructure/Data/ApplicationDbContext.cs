using Microsoft.EntityFrameworkCore;
using VoiceInputAssistant.Infrastructure.Data.Entities;

namespace VoiceInputAssistant.Infrastructure.Data;

/// <summary>
/// Entity Framework database context for the voice input assistant application
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Application profiles for different applications
    /// </summary>
    public DbSet<ApplicationProfileEntity> ApplicationProfiles { get; set; }

    /// <summary>
    /// Transcription events for usage tracking
    /// </summary>
    public DbSet<TranscriptionEventEntity> TranscriptionEvents { get; set; }

    /// <summary>
    /// Error events for monitoring system health
    /// </summary>
    public DbSet<ErrorEventEntity> ErrorEvents { get; set; }

    /// <summary>
    /// Engine performance metrics
    /// </summary>
    public DbSet<EngineMetricsEntity> EngineMetrics { get; set; }

    /// <summary>
    /// Users
    /// </summary>
    public DbSet<UserEntity> Users { get; set; }

    /// <summary>
    /// API keys
    /// </summary>
    public DbSet<ApiKeyEntity> ApiKeys { get; set; }

    /// <summary>
    /// User feedback on transcriptions
    /// </summary>
    public DbSet<UserFeedbackEntity> UserFeedbacks { get; set; }

    /// <summary>
    /// User-specific vocabulary entries
    /// </summary>
    public DbSet<UserVocabularyEntity> UserVocabularies { get; set; }

    /// <summary>
    /// Learned context patterns
    /// </summary>
    public DbSet<ContextPatternEntity> ContextPatterns { get; set; }

    /// <summary>
    /// User speech profiles
    /// </summary>
    public DbSet<SpeechProfileEntity> SpeechProfiles { get; set; }

    /// <summary>
    /// Confidence analysis results
    /// </summary>
    public DbSet<ConfidenceScoreEntity> ConfidenceScores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ApplicationProfileEntity
        modelBuilder.Entity<ApplicationProfileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ApplicationExecutables).HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
            );
            entity.Property(e => e.WindowTitlePatterns).HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
            );
            entity.Property(e => e.ProcessNamePatterns).HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
            );
            entity.Property(e => e.HotkeyConfigs).HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<VoiceInputAssistant.Core.Models.HotkeyConfig>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<VoiceInputAssistant.Core.Models.HotkeyConfig>()
            );
            entity.Property(e => e.TextProcessingRules).HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<VoiceInputAssistant.Core.Models.TextProcessingRule>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<VoiceInputAssistant.Core.Models.TextProcessingRule>()
            );
            entity.Property(e => e.SpeechRecognitionSettings).HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<VoiceInputAssistant.Core.Models.SpeechRecognitionSettings>(v, (System.Text.Json.JsonSerializerOptions?)null)
            );
        });

        // Configure TranscriptionEventEntity
        modelBuilder.Entity<TranscriptionEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.ProcessedText);
            entity.Property(e => e.ApplicationName).HasMaxLength(256);
            entity.Property(e => e.Metadata).HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null
            );
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.ApplicationName, e.Timestamp });
        });

        // Configure ErrorEventEntity
        modelBuilder.Entity<ErrorEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ErrorType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).IsRequired();
            entity.Property(e => e.StackTrace);
            entity.Property(e => e.ApplicationContext).HasMaxLength(256);
            entity.Property(e => e.AdditionalData).HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null
            );
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.ErrorType, e.Timestamp });
        });

        // Configure EngineMetricsEntity
        modelBuilder.Entity<EngineMetricsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EngineName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AdditionalMetrics).HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null
            );
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EngineName, e.Timestamp });
        });

        // Configure UserEntity
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Organization).HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Roles).HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure ApiKeyEntity
        modelBuilder.Entity<ApiKeyEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(256);
            entity.Property(e => e.KeyPrefix).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Scopes).HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ApiKeys)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
        });

        // Configure UserFeedbackEntity
        modelBuilder.Entity<UserFeedbackEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalText).IsRequired();
            entity.Property(e => e.CorrectedText).IsRequired();
            entity.Property(e => e.Engine).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ApplicationContext).HasMaxLength(256);
            entity.Property(e => e.FeedbackType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Metadata).HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null
            );
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.TranscriptionEvent)
                  .WithMany()
                  .HasForeignKey(e => e.TranscriptionEventId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => new { e.Engine, e.CreatedAt });
            entity.HasIndex(e => e.IsProcessed);
        });

        // Configure UserVocabularyEntity
        modelBuilder.Entity<UserVocabularyEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalWord).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CorrectedWord).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ApplicationContext).HasMaxLength(256);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Language).HasMaxLength(10);
            entity.Property(e => e.Metadata).HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null
            );
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.OriginalWord });
            entity.HasIndex(e => new { e.UserId, e.ApplicationContext });
            entity.HasIndex(e => e.IsActive);
        });

        // Configure ContextPatternEntity
        modelBuilder.Entity<ContextPatternEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecedingContext).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.FollowingContext).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ExpectedText).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ApplicationContext).HasMaxLength(256);
            entity.Property(e => e.PatternType).HasMaxLength(100);
            entity.Property(e => e.Language).HasMaxLength(10);
            entity.Property(e => e.Metadata).HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null
            );
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.PrecedingContext, e.FollowingContext });
            entity.HasIndex(e => new { e.UserId, e.ApplicationContext });
            entity.HasIndex(e => e.IsActive);
        });

        // Configure SpeechProfileEntity
        modelBuilder.Entity<SpeechProfileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProfileName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.PreferredEngine).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ApplicationContexts).HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );
            entity.Property(e => e.EngineSettings).HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null
            );
            entity.Property(e => e.AccuracyMetrics).HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, double>()
            );
            entity.Property(e => e.Metadata).HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null
            );
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.IsDefault });
            entity.HasIndex(e => new { e.UserId, e.ProfileName }).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // Configure ConfidenceScoreEntity
        modelBuilder.Entity<ConfidenceScoreEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.Engine).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ApplicationContext).HasMaxLength(256);
            entity.Property(e => e.WordConfidences).HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, double>()
            );
            entity.Property(e => e.UncertainWords).HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );
            entity.Property(e => e.AnalysisMetadata).HasConversion(
                v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                v => v != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) : null
            );
            entity.HasOne(e => e.TranscriptionEvent)
                  .WithMany()
                  .HasForeignKey(e => e.TranscriptionEventId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.TranscriptionEventId, e.AnalyzedAt });
            entity.HasIndex(e => new { e.Engine, e.OverallConfidence });
            entity.HasIndex(e => e.FeedbackRequested);
        });
    }
}
