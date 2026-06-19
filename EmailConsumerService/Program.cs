using Asp.Versioning;
using EmailConsumerService;
using EmailConsumerService.Configuration;
using EmailConsumerService.Services.Email;
using EmailConsumerService.Services.Email.Builders;
using EmailConsumerService.Services.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;
using SendGrid;
using Serilog;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // When hosted by the Windows Service Control Manager the working directory is
    // C:\Windows\System32, so anchor the content root to the executable's folder
    // to resolve appsettings.json and the relative log path correctly.
    ContentRootPath = WindowsServiceHelpers.IsWindowsService()
        ? AppContext.BaseDirectory
        : default
});

builder.Logging.ClearProviders();
builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "EmailConsumerService";
});

builder.Services.AddControllers();
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
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
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
    KafkaProducerFactory.Create(sp.GetRequiredService<KafkaOptions>()));
builder.Services.AddSingleton<IKafkaEmailProducer, KafkaEmailProducer>();
builder.Services.AddSingleton<SendGridClient>(sp =>
    new SendGridClient(sp.GetRequiredService<SendGridOptions>().ApiKey));
builder.Services.AddSingleton<ISmtpMailMessageFactory, SmtpMailMessageFactory>();
builder.Services.AddSingleton<ISendGridEmailSender, SendGridEmailSender>();
builder.Services.AddSingleton<ISmtpEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IEmailMessageHandler, EmailMessageHandler>();
builder.Services.AddSingleton<IKafkaEmailMessageProcessor, KafkaEmailMessageProcessor>();
builder.Services.AddSingleton<IKafkaEmailConsumer, KafkaEmailConsumer>();
builder.Services.AddHostedService<Worker>();

try
{
    var app = builder.Build();

    app.MapControllers();

    app.Run();
}
finally
{
    await Log.CloseAndFlushAsync();
}
