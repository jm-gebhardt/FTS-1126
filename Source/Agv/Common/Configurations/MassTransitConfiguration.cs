using Gebhardt.StoreWare.Agv.Common.Configurations.Attributes;
using Gebhardt.StoreWare.Agv.Common.Configurations.Models;

namespace Gebhardt.StoreWare.Agv.Common.Configurations;

/// <summary>
/// MassTransit Configuration required for RabbitMQ
/// </summary>
[AutoSeedToDatabase(false)]
public record MassTransitConfiguration : SettingsBase<MassTransitConfiguration>
{
    /// <inheritdoc/>
    public override string SectionName => "RabbitMQ";

    /// <summary>
    /// Host
    /// </summary>
    public string? Host { get; set; } = "rabbitmq";
    /// <summary>
    /// User
    /// </summary>
    public string? User { get; set; } = "unset";
    /// <summary>
    /// Password
    /// </summary>
    public string? Password { get; set; } = "PutYourPasswordHere";
    /// <summary>
    /// Port
    /// </summary>
    public string? Port { get; set; } = "5672";
    /// <summary>
    /// Create RabbitMQ URL
    /// </summary>
    public string Url => $"amqp://{User}:{Password}@{Host}:{Port}";
}