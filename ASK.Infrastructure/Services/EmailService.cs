using System.Net;
using System.Net.Mail;
using ASK.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ASK.Infrastructure.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken ct = default)
    {
        var host = configuration["Smtp:Host"];
        var portStr = configuration["Smtp:Port"];
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];
        var enableSslStr = configuration["Smtp:EnableSsl"];
        var fromName = configuration["Smtp:FromName"] ?? "ASK B2B System";
        var fromEmail = configuration["Smtp:FromEmail"] ?? username;

        // Fail-safe check: If SMTP host or credentials are not configured, log a warning and return.
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SMTP is not configured. Email to '{To}' with subject '{Subject}' was not sent.", to, subject);
            logger.LogInformation("SMTP MOCK LOG (Enable config in appsettings.json):\nTo: {To}\nSubject: {Subject}\nBody:\n{Body}", to, subject, body);
            return;
        }

        if (!int.TryParse(portStr, out var port))
        {
            port = 587;
        }

        if (!bool.TryParse(enableSslStr, out var enableSsl))
        {
            enableSsl = true;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail!, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(to);

            await client.SendMailAsync(message, ct);
            logger.LogInformation("Email successfully sent to '{To}' with subject '{Subject}'", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to '{To}' with subject '{Subject}' via SMTP host '{Host}'", to, subject, host);
        }
    }
}
