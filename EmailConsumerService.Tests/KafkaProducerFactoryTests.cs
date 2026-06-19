using Confluent.Kafka;
using EmailConsumerService.Configuration;
using EmailConsumerService.Services.Kafka;

namespace EmailConsumerService.Tests;

public class KafkaProducerFactoryTests
{
    private static KafkaOptions CreateOptions() => new()
    {
        BootstrapServers = "localhost:9092",
        Topic = "email-requests",
        GroupId = "email-consumer-service"
    };

    [Fact]
    public void CreateConfig_AppliesReliabilityDefaults()
    {
        var config = KafkaProducerFactory.CreateConfig(CreateOptions());

        Assert.Equal("localhost:9092", config.BootstrapServers);
        Assert.True(config.EnableIdempotence);
        Assert.Equal(Acks.All, config.Acks);
    }

    [Fact]
    public void CreateConfig_FallsBackToGroupIdForClientId()
    {
        var config = KafkaProducerFactory.CreateConfig(CreateOptions());

        Assert.Equal("email-consumer-service", config.ClientId);
    }

    [Fact]
    public void CreateConfig_UsesClientIdWhenProvided()
    {
        var options = CreateOptions();
        options.ClientId = "custom-client";

        var config = KafkaProducerFactory.CreateConfig(options);

        Assert.Equal("custom-client", config.ClientId);
    }

    [Theory]
    [InlineData("all", Acks.All)]
    [InlineData("-1", Acks.All)]
    [InlineData("leader", Acks.Leader)]
    [InlineData("1", Acks.Leader)]
    [InlineData("none", Acks.None)]
    [InlineData("0", Acks.None)]
    [InlineData("unrecognized", Acks.All)]
    public void CreateConfig_ParsesAcks(string value, Acks expected)
    {
        var options = CreateOptions();
        options.Acks = value;

        var config = KafkaProducerFactory.CreateConfig(options);

        Assert.Equal(expected, config.Acks);
    }

    [Theory]
    [InlineData("gzip", CompressionType.Gzip)]
    [InlineData("snappy", CompressionType.Snappy)]
    [InlineData("lz4", CompressionType.Lz4)]
    [InlineData("zstd", CompressionType.Zstd)]
    public void CreateConfig_ParsesCompressionType(string value, CompressionType expected)
    {
        var options = CreateOptions();
        options.CompressionType = value;

        var config = KafkaProducerFactory.CreateConfig(options);

        Assert.Equal(expected, config.CompressionType);
    }

    [Fact]
    public void CreateConfig_LeavesOptionalSettingsUnsetByDefault()
    {
        var config = KafkaProducerFactory.CreateConfig(CreateOptions());

        Assert.Null(config.CompressionType);
        Assert.Null(config.MessageMaxBytes);
    }

    [Fact]
    public void CreateConfig_SetsMessageMaxBytesWhenProvided()
    {
        var options = CreateOptions();
        options.MessageMaxBytes = 5_242_880;

        var config = KafkaProducerFactory.CreateConfig(options);

        Assert.Equal(5_242_880, config.MessageMaxBytes);
    }

    [Fact]
    public void CreateConfig_WhenBootstrapServersMissing_Throws()
    {
        var options = CreateOptions();
        options.BootstrapServers = "";

        var exception = Assert.Throws<InvalidOperationException>(
            () => KafkaProducerFactory.CreateConfig(options));

        Assert.Equal("Kafka:BootstrapServers must be configured.", exception.Message);
    }

    [Fact]
    public void CreateConfig_WhenTopicMissing_Throws()
    {
        var options = CreateOptions();
        options.Topic = "";

        var exception = Assert.Throws<InvalidOperationException>(
            () => KafkaProducerFactory.CreateConfig(options));

        Assert.Equal("Kafka:Topic must be configured.", exception.Message);
    }
}
