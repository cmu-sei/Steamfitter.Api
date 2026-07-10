// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using STT = System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Steamfitter.Api.Infrastructure.Options;

namespace Steamfitter.Api.Services
{
    public interface IEmailService
    {
        STT.Task<string> SendEmail(string parameters);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IOptionsMonitor<EmailOptions> _emailOptions;

        public EmailService(
            ILogger<EmailService> logger,
            IOptionsMonitor<EmailOptions> emailOptions)
        {
            _logger = logger;
            _emailOptions = emailOptions;
        }

        public async STT.Task<string> SendEmail(string parameters)
        {
            var p = JsonSerializer.Deserialize<SendEmailParameters>(parameters);
            var options = _emailOptions.CurrentValue;

            if (string.IsNullOrEmpty(options.SmtpHost))
                throw new InvalidOperationException("Email:SmtpHost is not configured.");

            var fromAddress = !string.IsNullOrEmpty(p.EmailFrom) ? p.EmailFrom : options.DefaultFromAddress;
            if (string.IsNullOrEmpty(fromAddress))
                throw new InvalidOperationException("Sender address is required (EmailFrom param or Email:DefaultFromAddress).");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromAddress));
            foreach (var to in SplitAddresses(p.EmailTo))
                message.To.Add(MailboxAddress.Parse(to));
            foreach (var cc in SplitAddresses(p.EmailCC))
                message.Cc.Add(MailboxAddress.Parse(cc));

            message.Subject = p.Subject ?? string.Empty;

            var body = new BodyBuilder();
            var isHtml = string.Equals(p.Mime, "html", StringComparison.OrdinalIgnoreCase);
            if (isHtml) body.HtmlBody = p.Message;
            else body.TextBody = p.Message;

            foreach (var path in SplitAddresses(p.AttachmentPaths))
            {
                if (File.Exists(path)) body.Attachments.Add(path);
                else _logger.LogWarning("Email attachment not found: {path}", path);
            }
            message.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            if (options.AcceptAllCertificates)
                client.ServerCertificateValidationCallback = (_, _, _, _) => true;

            var secureOption = options.UseStartTls ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.Auto;
            await client.ConnectAsync(options.SmtpHost, options.SmtpPort, secureOption);

            if (!string.IsNullOrEmpty(options.SmtpUsername))
                await client.AuthenticateAsync(options.SmtpUsername, options.SmtpPassword);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return "True";
        }

        private static System.Collections.Generic.IEnumerable<string> SplitAddresses(string s) =>
            string.IsNullOrEmpty(s)
                ? Enumerable.Empty<string>()
                : s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

        private class SendEmailParameters
        {
            public string Account { get; set; }
            public string EmailFrom { get; set; }
            public string EmailTo { get; set; }
            public string EmailCC { get; set; }
            public string Subject { get; set; }
            public string Message { get; set; }
            public string Mime { get; set; }
            public string AttachmentPaths { get; set; }
        }
    }
}
