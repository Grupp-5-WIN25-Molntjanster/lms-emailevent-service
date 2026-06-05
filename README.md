Owner: Albin Nilsson

### API for sending verification emails via Azure Service Bus and Azure Communication Services.

### Features
- Listens to Azure Service Bus queue for verification email requests
- Sends verification codes via Azure Communication Services Email
- Background worker runs continuously (backgroundService)
- Diagnostics endpoints (connection sometimes get lost or is slow on Azure deployment)
- Custom retry policy for throttling (429) (get info from Azure when serice is throttling)
- Sender test console app (to queue messages to Azure Service Bus so they can get picked up by worker)

### Tech Stack
- C#
- .NET 10.0
- ASP.NET Core Minimal API
- Azure Service Bus
- Azure Communication Services Email
- Preferably a custom domain (Azure free has limits on emails sent)

### API Endpoints
| Method | Route        | Description                                                |
| ------ | ------------ | -----------------------------------------------------------|
| GET    | `/health`    | Returns health status with timestamp                       |
| GET    | `/diag`      | Peek Service Bus queue + validate ACS connection           |
| GET    | `/diag/worker`| Receive 1 message from Service bus queue                  |
| GET    | `/sendtest`  | Send a hardcoded test verification email (see sendertest)  |

This service communicates via Azure Service Bus, it has no endpoints for frontend or other service to use. The endpoints above are for diagnostics only.

### How to run
#### Requirements
- .NET 10.0 SDK
- Azure Service Bus with queue `verification-email`
- Azure Communication Services with email domain
- Connection strings, queue name and sender adress configured in appsettings.json
#### Variables
- `Azure:CommunicationServices:ConnectionString`
- `Azure:CommunicationServices:SenderAddress`
- `Azure:ServiceBus:ConnectionString`
- `Azure:ServiceBus:QueueName`

#### Run locally
- cd (your repo clone location)/lms-emailevent-service/src/EmailEvent.Api
- dotnet restore
- dotnet run
- 
Open http://localhost:5064/health to verify service is running.

### Azure Deployment
The service is deployed to Azure via GitHub Actions CI/CD.
URL: http://lms-emailevent-service.azurewebsites.net

### Frontend
This service has no frontend component. The flow is: Frontend, Auth Service, Verification Serivce, Service Bus queue, EmailEvent Service, Azure Communication Services Email, User inbox.
