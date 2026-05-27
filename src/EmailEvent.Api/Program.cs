using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using EmailEvent.Api.Workers;
using EmailEvent.Core.Interfaces;
using EmailEvent.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Azure Communication Services Email
var acsConnectionString = builder.Configuration["Azure:CommunicationServices:ConnectionString"]
    ?? throw new InvalidOperationException("Azure:CommunicationServices:ConnectionString is not configured");

var senderAddress = builder.Configuration["Azure:CommunicationServices:SenderAddress"]
    ?? throw new InvalidOperationException("Azure:CommunicationServices:SenderAddress is not configured");


builder.Services.AddSingleton(new EmailClient(acsConnectionString));

builder.Services.AddSingleton<IEmailSender>(sp =>
{
    var client = sp.GetRequiredService<EmailClient>();
    return new EmailSender(client, senderAddress);
});


// Service Bus
var sbConnectionString = builder.Configuration["Azure:ServiceBus:ConnectionString"]
    ?? throw new InvalidOperationException("Azure:ServiceBus:ConnectionString is not configured");

var queueName = builder.Configuration["Azure:ServiceBus:QueueName"]
    ?? throw new InvalidOperationException("Azure:ServiceBus:QueueName is not configured");

builder.Services.AddSingleton(new ServiceBusClient(sbConnectionString));


// Core services
builder.Services.AddScoped<MessageProcessor>();

// Background worker
builder.Services.AddHostedService<EmailWorker>();
builder.Services.AddApplicationInsightsTelemetry();


var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapGet("/diag", async (IConfiguration config) =>
{
    var results = new List<object>();
    try
    {
        var sbConn = config["Azure:ServiceBus:ConnectionString"]!;
        var queue = config["Azure:ServiceBus:QueueName"]!;
        await using var client = new ServiceBusClient(sbConn);
        var receiver = client.CreateReceiver(queue);
        var peeked = await receiver.PeekMessageAsync();
        results.Add(new { service = "ServiceBus", status = "OK", canPeek = peeked is not null, peekedMessageId = peeked?.MessageId });
    }
    catch (Exception ex)
    {
        results.Add(new { service = "ServiceBus", status = "FAIL", error = ex.Message });
    }
    try
    {
        var acsConn = config["Azure:CommunicationServices:ConnectionString"]!;
        var sender = config["Azure:CommunicationServices:SenderAddress"]!;
        var client = new EmailClient(acsConn);
        results.Add(new { service = "ACS", status = "OK", senderAddress = sender });
    }
    catch (Exception ex)
    {
        results.Add(new { service = "ACS", status = "FAIL", error = ex.Message });
    }
    return Results.Ok(results);
});
app.Run();