using Azure.Messaging.ServiceBus;
using EmailEvent.Core.Services;
using Microsoft.Extensions.Logging;
namespace EmailEvent.Api.Workers;

public class EmailWorker(
    ServiceBusClient serviceBusClient,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EmailWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var queueName = configuration["Azure:ServiceBus:QueueName"]!;
        var receiver = serviceBusClient.CreateReceiver(queueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });

        
        logger.LogInformation("EmailWorker started, polling queue: {QueueName}", queueName);
        
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10), ct);
                if (message is null)
                {
                    await Task.Delay(1000, ct);
                    continue;
                }

                logger.LogInformation("Message received. MessageId: {MessageId}", message.MessageId);
                
                using var scope = scopeFactory.CreateScope();
                
                var processor = scope.ServiceProvider.GetRequiredService<MessageProcessor>();
                
                try
                {
                    await processor.ProcessMessageAsync(message, ct);
                    await receiver.CompleteMessageAsync(message, ct);
                    logger.LogInformation("Message completed. MessageId: {MessageId}", message.MessageId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message {MessageId}, abandoning", message.MessageId);
                    await receiver.AbandonMessageAsync(message, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in polling loop");
                await Task.Delay(5000, ct);
            }
        }
    }
}