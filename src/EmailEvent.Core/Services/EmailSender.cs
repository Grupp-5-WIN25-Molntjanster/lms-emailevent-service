using Azure;
using Azure.Communication.Email;
using Azure.Core;
using Azure.Core.Pipeline;
using EmailEvent.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace EmailEvent.Core.Services;

public class Catch429Policy : HttpPipelinePolicy
{
    public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        ProcessNext(message, pipeline);
        if (message.Response.Status == 429)
            throw new RequestFailedException(message.Response);
    }
    public override async ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        await ProcessNextAsync(message, pipeline);
        if (message.Response.Status == 429)
            throw new RequestFailedException(message.Response);
    }
}
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
                var options = new EmailClientOptions();
                options.AddPolicy(new Catch429Policy(), HttpPipelinePosition.PerRetry);
                options.Retry.MaxRetries = 0;
                var client = new EmailClient(acsConnectionString, options);
                await client.SendAsync(WaitUntil.Started, emailMessage, sendCts.Token);
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
                var retryAfter = ex.GetRawResponse()?.Headers
                    .Where(h => h.Name.Equals("Retry-After", StringComparison.OrdinalIgnoreCase))
                    .Select(h => h.Value)
                    .FirstOrDefault();
                if (retryAfter is not null && int.TryParse(retryAfter, out var seconds))
                {
                    logger.LogWarning("ACS throttled. Retry-After: {Seconds}s (attempt {Attempt})", seconds, attempt);
                    await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
                }
                else
                {
                    var delay = attempt * 10;
                    logger.LogWarning("ACS throttled (no Retry-After). Waiting {Delay}s (attempt {Attempt})", delay, attempt);
                    await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                }
            }
        }
    }
}