using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.Infrastructure.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Impl
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly IWebHostEnvironment _env;

        public EmailService(IOptions<EmailSettings> settings, IWebHostEnvironment env)
        {
            _settings = settings.Value;
            _env = env;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
        {
            var body = await LoadTemplateAsync("password-reset", new Dictionary<string, string>
        {
            { "{{UserName}}", userName },
            { "{{ResetLink}}", resetLink }
        });

            await SendEmailAsync(toEmail, "Відновлення паролю — CaramelCloud", body);
        }


        private async Task<string> LoadTemplateAsync(string templateName,
            Dictionary<string, string> replacements)
        {
            var path = Path.Combine(
                _env.WebRootPath, "email-templates", $"{templateName}.html");

            var template = await File.ReadAllTextAsync(path);

            foreach (var (placeholder, value) in replacements)
                template = template.Replace(placeholder, value);

            return template;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password),
                EnableSsl = true
            };

            var msg = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail);

            await client.SendMailAsync(msg);
        }
    }
}
