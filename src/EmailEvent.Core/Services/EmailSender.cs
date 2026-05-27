using Azure;
using Azure.Communication.Email;
using EmailEvent.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
namespace EmailEvent.Core.Services;

public class EmailSender(string acsConnectionString, string senderAddress, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendVerificationCodeAsync(string email, string code, CancellationToken ct)
    {
        var emailMessage = new EmailMessage(
            senderAddress: senderAddress,
            content: new EmailContent("Your Verification Code for Shiko Learning")
            {
                PlainText = $"Your verification code is: {code}",
                Html = $"<h2>Verification Code</h2><p>Your code is: <strong>{code}</strong></p>"
            },
            recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(email) }));
        int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                sendCts.CancelAfter(TimeSpan.FromSeconds(15));
                await new EmailClient(acsConnectionString).SendAsync(WaitUntil.Started, emailMessage, sendCts.Token);
                return;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("ACS send timed out (attempt {Attempt})", attempt);
                if (attempt >= maxRetries) throw;
            }
            catch (RequestFailedException ex) when (ex.Status == 429)
            {
                if (attempt >= maxRetries) throw;
                var delay = attempt * 5;
                logger.LogWarning("ACS throttled (attempt {Attempt}). Waiting {Delay}s...", attempt, delay);
                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
            }
        }
    }
}