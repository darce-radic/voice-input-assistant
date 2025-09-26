using System.Collections.Generic;
using System.Threading.Tasks;

namespace VoiceInputAssistant.Core.Services.Interfaces;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email
    /// </summary>
    Task<bool> SendEmailAsync(EmailMessage message);
    
    /// <summary>
    /// Sends a password reset email
    /// </summary>
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken);
    
    /// <summary>
    /// Sends a welcome email to new users
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string toEmail, string username);
    
    /// <summary>
    /// Sends an email confirmation message
    /// </summary>
    Task SendEmailConfirmationAsync(string email, string confirmationLink);
    
    /// <summary>
    /// Sends a password reset email
    /// </summary>
    Task SendPasswordResetAsync(string email, string resetLink);
}

/// <summary>
/// Email message
/// </summary>
public record EmailMessage(
    string To,
    string Subject,
    string Body,
    bool IsHtml = false,
    IEnumerable<EmailAttachment>? Attachments = null
);

/// <summary>
/// Email attachment
/// </summary>
public record EmailAttachment(
    string FileName,
    byte[] Content,
    string ContentType
);