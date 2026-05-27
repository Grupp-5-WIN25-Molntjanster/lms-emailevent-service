using System;
using System.Collections.Generic;
using System.Text;
using Azure;
using Azure.Communication.Email;
using EmailEvent.Core.Interfaces;
namespace EmailEvent.Core.Services;

public class EmailSender(EmailClient emailClient, string senderAddress) : IEmailSender
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
                await emailClient.SendAsync(WaitUntil.Completed, emailMessage, ct);
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == 429 && attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
            }
        }
        await emailClient.SendAsync(WaitUntil.Completed, emailMessage, ct);
    }
}