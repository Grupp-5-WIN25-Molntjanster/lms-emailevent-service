using Azure.Messaging.ServiceBus;
using EmailEvent.Core.DTOs;
using EmailEvent.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
namespace EmailEvent.Core.Services;

public class MessageProcessor(IEmailSender emailSender, ILogger<MessageProcessor> logger)
{
    public async Task ProcessMessageAsync(ServiceBusReceivedMessage message, CancellationToken ct)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var verificationMessage = JsonSerializer.Deserialize<VerificationMessage>(message.Body.ToString(), options);

        if (verificationMessage is null)
        {
            logger.LogError("Failed to deserialize message body.");
            return;
        }

        if (string.IsNullOrWhiteSpace(verificationMessage.Email) || string.IsNullOrWhiteSpace(verificationMessage.Code))
        {
            logger.LogError("Invalid message: Email or Code is empty.");
            return;
        }

        await emailSender.SendVerificationCodeAsync(verificationMessage.Email, verificationMessage.Code, ct);
        logger.LogInformation("Sent verification email to: {Email}", verificationMessage.Email);
    }
}
