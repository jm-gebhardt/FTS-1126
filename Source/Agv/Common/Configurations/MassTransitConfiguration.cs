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
    /// The schema used for the RabbitMQ management API URL (e.g., "http", "https").
    /// </summary>
    public string? Schema { get; init; } = "http";

    /// <summary>
    /// Host
    /// </summary>
    public string? Host { get; init; } = "rabbitmq";

    /// <summary>
    /// User
    /// </summary>
    public string? User { get; init; } = "unset";

    /// <summary>
    /// Management API Port
    /// </summary>
    public ushort? ManagementApiPort { get; init; } = 15672;

    /// <summary>
    /// Password
    /// </summary>
    public string? Password { get; init; } = "PutYourPasswordHere";

    /// <summary>
    /// Port
    /// </summary>
    public ushort? Port { get; init; } = 5672;

    /// <summary>
    /// Create RabbitMQ URL
    /// </summary>
    public string Url => $"amqp://{User}:{Uri.EscapeDataString(Password!)}@{Host}:{Port}";

    /// <summary>
    /// Path to check certificate expiration
    /// </summary>
    public string ExpirationCheckPath { get; init; } = "/api/health/checks/certificate-expiration/1000/years";
}