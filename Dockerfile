# Build and run (from repository root):
#   docker build -t email-consumer-service .
#   docker run --rm \
#     -e Kafka__BootstrapServers=kafka:9092 \
#     -e Kafka__Topic=email-requests \
#     -e SendGrid__ApiKey=your-api-key \
#     -e Smtp__Host=smtp.example.com \
#     email-consumer-service

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EmailConsumerService/EmailConsumerService.csproj EmailConsumerService/
RUN dotnet restore EmailConsumerService/EmailConsumerService.csproj

COPY EmailConsumerService/ EmailConsumerService/
RUN dotnet publish EmailConsumerService/EmailConsumerService.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

ENV DOTNET_ENVIRONMENT=Production

RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EmailConsumerService.dll"]
