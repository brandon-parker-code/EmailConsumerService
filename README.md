# Email Consumer Service

A .NET 10 worker that consumes email requests from a Kafka topic and sends them via SendGrid, with automatic SMTP fallback when SendGrid fails.

## How it works

1. The service subscribes to a Kafka topic and reads JSON email messages.
2. Each message is deserialized into an `EmailMessage` object (including attachments).
3. The handler validates the message and attempts delivery through SendGrid.
4. If SendGrid throws, the same message is retried through SMTP.
5. Kafka offsets are committed only after a message is sent successfully.

```
Kafka topic → KafkaEmailConsumer → KafkaEmailMessageProcessor → EmailMessageHandler
                                                                      ↓
                                                            SendGridEmailSender
                                                                      ↓ (on failure)
                                                               SmtpEmailSender
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (to build from source)
- [Docker](https://www.docker.com/) (optional, for Linux container deployment)
- A Kafka cluster reachable from the host running this service
- A SendGrid API key (primary delivery)
- SMTP credentials (fallback delivery)
- Windows Server or Windows 10/11 (for running as a Windows Service)

## Configuration

Settings are read from `appsettings.json`, environment-specific files (for example `appsettings.Production.json`), and environment variables.

| Section | Setting | Description |
|---------|---------|-------------|
| `Kafka` | `BootstrapServers` | Kafka broker address (for example `localhost:9092`) |
| `Kafka` | `Topic` | Topic to consume |
| `Kafka` | `GroupId` | Consumer group id |
| `Kafka` | `AutoOffsetReset` | `Earliest` or `Latest` |
| `SendGrid` | `ApiKey` | SendGrid API key |
| `SendGrid` | `DefaultFromEmail` | Default sender address |
| `SendGrid` | `DefaultFromName` | Default sender display name |
| `Smtp` | `Host` | SMTP server host |
| `Smtp` | `Port` | SMTP port (commonly `587`) |
| `Smtp` | `Username` | SMTP username (optional) |
| `Smtp` | `Password` | SMTP password (optional) |
| `Smtp` | `EnableSsl` | Use TLS |
| `Smtp` | `DefaultFromEmail` | Fallback sender address |
| `Smtp` | `DefaultFromName` | Fallback sender display name |

Environment variables use the `__` (double underscore) delimiter. For example:

```powershell
$env:SendGrid__ApiKey = "your-sendgrid-api-key"
$env:Kafka__BootstrapServers = "kafka.example.com:9092"
$env:Smtp__Host = "smtp.example.com"
$env:Smtp__Username = "smtp-user"
$env:Smtp__Password = "smtp-password"
```

For local development, you can also use .NET user secrets:

```powershell
cd EmailConsumerService
dotnet user-secrets set "SendGrid:ApiKey" "your-sendgrid-api-key"
```

Do not store production secrets in source control. Use environment variables, a secure secret store, or an `appsettings.Production.json` file that is excluded from git.

## Kafka message format

Messages must be JSON with this shape:

```json
{
  "to": ["recipient@example.com"],
  "from": "sender@example.com",
  "fromName": "Sender Name",
  "cc": [],
  "bcc": [],
  "subject": "Hello",
  "body": "<p>Email body</p>",
  "isHtml": true,
  "attachments": [
    {
      "fileName": "document.pdf",
      "contentType": "application/pdf",
      "contentBase64": "base64-encoded-content"
    }
  ]
}
```

- `to` and `subject` are required.
- `from` is optional if `DefaultFromEmail` is configured for SendGrid/SMTP.
- Attachments require `fileName` and `contentBase64`.

## Local development

Run from the project folder:

```powershell
cd EmailConsumerService
dotnet run
```

Run tests:

```powershell
dotnet test EmailConsumerService.slnx
```

## Run with Docker

The `Dockerfile` builds a **Linux** image based on **Ubuntu 24.04** (Microsoft's default .NET 10 container base). The same application can also run as a Windows Service on Windows without Docker.

Build the image from the repository root:

```powershell
docker build -t email-consumer-service .
```

Run the container (pass configuration via environment variables):

```powershell
docker run --rm `
  -e Kafka__BootstrapServers=kafka:9092 `
  -e Kafka__Topic=email-requests `
  -e Kafka__GroupId=email-consumer-service `
  -e SendGrid__ApiKey=your-sendgrid-api-key `
  -e SendGrid__DefaultFromEmail=noreply@example.com `
  -e Smtp__Host=smtp.example.com `
  -e Smtp__Port=587 `
  -e Smtp__Username=smtp-user `
  -e Smtp__Password=smtp-password `
  email-consumer-service
```

Or mount a config file instead of individual variables:

```powershell
docker run --rm `
  -v C:\path\to\appsettings.Production.json:/app/appsettings.Production.json:ro `
  email-consumer-service
```

Notes:

- The container runs as a non-root `appuser`.
- `DOTNET_ENVIRONMENT` defaults to `Production` in the image.
- Windows Service hosting (`AddWindowsService`) is ignored on Linux; it only applies when running on Windows.
- Ensure the container can reach Kafka, SendGrid, and SMTP over the network (Docker network, DNS, and firewall rules).

## Publish the application

Publish a release build to a fixed folder on the server:

```powershell
dotnet publish EmailConsumerService\EmailConsumerService.csproj `
  -c Release `
  -o C:\Services\EmailConsumerService
```

The publish folder should contain:

- `EmailConsumerService.exe`
- `appsettings.json`
- Dependent assemblies

Update `appsettings.json` in the publish folder (or add `appsettings.Production.json`) with production values before installing the service.

## Install as a Windows Service

Use this path when deploying natively on Windows (without Docker).

The app registers itself for Windows Service hosting with the service name **`EmailConsumerService`**.

Open PowerShell **as Administrator**.

### Create the service

```powershell
sc.exe create EmailConsumerService `
  binPath= "C:\Services\EmailConsumerService\EmailConsumerService.exe" `
  start= auto `
  DisplayName= "Email Consumer Service"
```

Notes:

- There must be a space after `binPath=` and after `start=`.
- Use the full path to the published executable.
- Set `start= auto` to start the service when Windows boots.

### Configure the service account (recommended)

By default, services run as Local System. For production, use an account that can reach Kafka, SendGrid, and SMTP:

```powershell
sc.exe config EmailConsumerService obj= "DOMAIN\ServiceAccount" password= "YourPassword"
```

Or configure the log-on account in **services.msc** → **Email Consumer Service** → **Properties** → **Log On**.

### Start and verify

```powershell
sc.exe start EmailConsumerService
sc.exe query EmailConsumerService
```

View recent events in **Event Viewer** → **Windows Logs** → **Application**, or run the app from a console first to verify configuration.

### Stop and remove

```powershell
sc.exe stop EmailConsumerService
sc.exe delete EmailConsumerService
```

## Project structure

```
EmailConsumerService/
├── EmailConsumerService/          # Worker application
│   ├── Configuration/             # Options classes
│   ├── Models/                    # EmailMessage, EmailAttachment
│   └── Services/
│       ├── Email/                 # Handlers and senders
│       │   └── Builders/          # SMTP MailMessage builder
│       └── Kafka/                 # Consumer and message processor
└── EmailConsumerService.Tests/    # Unit tests
```

## Troubleshooting

| Issue | Things to check |
|-------|-----------------|
| Container exits immediately | `docker logs <container>`; verify Kafka/SendGrid/SMTP env vars |
| Service starts then stops | Kafka/SendGrid/SMTP settings; run the exe from a console to see errors |
| Messages not sent | Kafka connectivity, topic name, consumer group, API key, SMTP host |
| Messages retry repeatedly | SendGrid and SMTP both failing; failed messages are not committed to Kafka |
| Attachments fail | Valid base64, non-empty `fileName`, correct `contentType` |
