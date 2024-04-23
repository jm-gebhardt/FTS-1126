using Gebhardt.Acc.Common.Configurations.Attributes;
using Gebhardt.Acc.Common.Configurations.Models;

namespace Gebhardt.Acc.Common.Configurations;

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
    public string? Host { get; init; } = "rabbitmq";

    /// <summary>
    /// User
    /// </summary>
    public string? User { get; init; } = "unset";

    /// <summary>
    /// Password
    /// </summary>
    public string? Password { get; init; } = "PutYourPasswordHere";

    /// <summary>
    /// Port
    /// </summary>
    public string? Port { get; init; } = "5672";

    /// <summary>
    /// Create RabbitMQ URL
    /// </summary>
    public string Url => $"amqp://{User}:{Uri.EscapeDataString(Password!)}@{Host}:{Port}";
}