using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;


var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();


var connectionString = config["Azure:ServiceBus:ConnectionString"]
    ?? throw new InvalidOperationException("Missing ConnectionString");

var queueName = config["Azure:ServiceBus:QueueName"]
    ?? throw new InvalidOperationException("Missing QueueName");


var email = args.Length > 0 ? args[0] : "wkwwd78046@minitts.net";
var code = args.Length > 1 ? args[1] : "13371337";

var messageBody = $$"""{"email":"{{email}}","code":"{{code}}"}""";

await using var client = new ServiceBusClient(connectionString);
ServiceBusSender sender = client.CreateSender(queueName);
await sender.SendMessageAsync(new ServiceBusMessage(messageBody));

Console.WriteLine($"Sent verification code {code} to {email}");