namespace VoiceInputAssistant.Infrastructure.Configuration;

/// <summary>
/// Email service configuration settings
/// </summary>
public class EmailSettings
{
    public bool IsEnabled { get; set; } = false;
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool UseSsl { get; set; } = true;
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "";
    public string BaseUrl { get; set; } = "";
}

/// <summary>
/// Whisper service configuration settings
/// </summary>
public class WhisperSettings
{
    public string ModelPath { get; set; } = "";
    public string Language { get; set; } = "en";
    public float Temperature { get; set; } = 0.0f;
    public bool Translate { get; set; } = false;
    public int MaxTokens { get; set; } = 224;
}

/// <summary>
/// Token service configuration settings
/// </summary>
public class TokenSettings
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "VoiceInputAssistant";
    public string Audience { get; set; } = "VoiceInputAssistant";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 30;
    public string SecretKey => Key; // Alias for backward compatibility
}