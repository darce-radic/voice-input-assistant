using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceInputAssistant.Core.Services.Interfaces;
using VoiceInputAssistant.Infrastructure.Configuration;

namespace VoiceInputAssistant.Infrastructure.Services;

/// <summary>
/// SMTP-based email service implementation
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    public async Task<bool> SendEmailAsync(EmailMessage message)
    {
        try
        {
            if (!_emailSettings.IsEnabled)
            {
                _logger.LogInformation("Email service is disabled. Message would have been sent to {To} with subject '{Subject}'", 
                    message.To, message.Subject);
                return true; // Return true in dev/test to not fail auth workflows
            }

            using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                EnableSsl = _emailSettings.UseSsl
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsHtml
            };

            mailMessage.To.Add(message.To);

            // Add attachments if any
            if (message.Attachments != null)
            {
                foreach (var attachment in message.Attachments)
                {
                    var mailAttachment = new Attachment(new System.IO.MemoryStream(attachment.Content), attachment.FileName, attachment.ContentType);
                    mailMessage.Attachments.Add(mailAttachment);
                }
            }

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'", message.To, message.Subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'", message.To, message.Subject);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var resetLink = $"{_emailSettings.BaseUrl}/reset-password?token={resetToken}&email={toEmail}";
        var subject = "Password Reset Request - Voice Input Assistant";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Password Reset Request</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 40px;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
        <h2 style='color: #333; text-align: center;'>Password Reset Request</h2>
        <p>We received a request to reset your password for your Voice Input Assistant account.</p>
        <p>Click the link below to reset your password:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{resetLink}' style='background: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>Reset Password</a>
        </div>
        <p style='color: #666; font-size: 14px;'>If you didn't request this password reset, please ignore this email. The link will expire in 24 hours.</p>
        <p style='color: #666; font-size: 14px;'>If the button above doesn't work, copy and paste this link into your browser:</p>
        <p style='word-break: break-all; color: #666; font-size: 12px;'>{resetLink}</p>
    </div>
</body>
</html>";

        var message = new EmailMessage(toEmail, subject, body, IsHtml: true);
        return await SendEmailAsync(message);
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string username)
    {
        var subject = "Welcome to Voice Input Assistant!";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Welcome to Voice Input Assistant</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 40px;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
        <h2 style='color: #333; text-align: center;'>Welcome to Voice Input Assistant!</h2>
        <p>Hello {username},</p>
        <p>Thank you for signing up for Voice Input Assistant! We're excited to help you improve your productivity with voice-to-text capabilities.</p>
        <h3>Getting Started:</h3>
        <ul>
            <li>Download the desktop application</li>
            <li>Configure your preferred speech recognition engine</li>
            <li>Set up application profiles for different software</li>
            <li>Start using voice commands to boost your productivity!</li>
        </ul>
        <p>If you have any questions or need help getting started, please don't hesitate to contact our support team.</p>
        <p>Happy dictating!</p>
        <p><strong>The Voice Input Assistant Team</strong></p>
    </div>
</body>
</html>";

        var message = new EmailMessage(toEmail, subject, body, IsHtml: true);
        return await SendEmailAsync(message);
    }

    public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        var subject = "Confirm Your Email - Voice Input Assistant";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Email Confirmation</title>
</head>
<body style='font-family: Arial, sans-serif; margin: 40px;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
        <h2 style='color: #333; text-align: center;'>Email Confirmation</h2>
        <p>Thank you for signing up for Voice Input Assistant!</p>
        <p>Please confirm your email address by clicking the link below:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{confirmationLink}' style='background: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>Confirm Email</a>
        </div>
        <p style='color: #666; font-size: 14px;'>If you didn't create this account, please ignore this email.</p>
        <p style='color: #666; font-size: 14px;'>If the button above doesn't work, copy and paste this link into your browser:</p>
        <p style='word-break: break-all; color: #666; font-size: 12px;'>{confirmationLink}</p>
    </div>
</body>
</html>";

        var message = new EmailMessage(email, subject, body, IsHtml: true);
        await SendEmailAsync(message);
    }

    public async Task SendPasswordResetAsync(string email, string resetLink)
    {
        await SendPasswordResetEmailAsync(email, resetLink);
    }
}

/// <summary>
/// Email service configuration settings
/// </summary>
public class EmailSettings
{
    public bool IsEnabled { get; set; } = false;
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Voice Input Assistant";
    public string BaseUrl { get; set; } = "https://localhost:5001";
}