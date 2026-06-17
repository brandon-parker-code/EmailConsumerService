using EmailConsumerService;
using EmailConsumerService.Configuration;
using EmailConsumerService.Services.Email;
using EmailConsumerService.Services.Email.Builders;
using EmailConsumerService.Services.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using SendGrid;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "EmailConsumerService";
});

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.Configure<SendGridOptions>(
    builder.Configuration.GetSection(SendGridOptions.SectionName));
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<KafkaOptions>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SendGridOptions>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SmtpOptions>>().Value);

builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
    KafkaConsumerFactory.Create(sp.GetRequiredService<KafkaOptions>()));
builder.Services.AddSingleton<SendGridClient>(sp =>
    new SendGridClient(sp.GetRequiredService<SendGridOptions>().ApiKey));
builder.Services.AddSingleton<ISmtpMailMessageFactory, SmtpMailMessageFactory>();
builder.Services.AddSingleton<ISendGridEmailSender, SendGridEmailSender>();
builder.Services.AddSingleton<ISmtpEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IEmailMessageHandler, EmailMessageHandler>();
builder.Services.AddSingleton<IKafkaEmailMessageProcessor, KafkaEmailMessageProcessor>();
builder.Services.AddSingleton<IKafkaEmailConsumer, KafkaEmailConsumer>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
