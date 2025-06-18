using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.RabbitMQ.Api;

/// Represents the response from RabbitMQ containing certificate expiration details.
[UsedImplicitly]
public class RabbitMqExpirationResponse
{
    /// Represents the status of the RabbitMQ certificate expiration response.
    [UsedImplicitly]
    public string Status
    {
        get;
        [UsedImplicitly]
        init;
    } = null!;

    /// Provides the reason for the RabbitMQ certificate expiration status.
    [UsedImplicitly]
    public string Reason
    {
        get;
        [UsedImplicitly]
        init;
    } = null!;

    /// Represents the list of certificates reported as expired by the RabbitMQ API.
    [UsedImplicitly]
    public List<CertApiResponses> Expired
    {
        get;
        [UsedImplicitly]
        init;
    } = null!;
}

/// Represents API response details for certificate expiration associated with RabbitMQ.
[UsedImplicitly]
public class CertApiResponses
{
    /// Represents the file path of the RabbitMQ certificate.
    [JsonPropertyName("certfile")]
    public string CertFile
    {
        get;
        [UsedImplicitly]
        init;
    } = null!;

    /// Represents the expiration dates of the RabbitMQ certificate file.
    [JsonPropertyName("certfile_expires_on")]
    [JsonConverter(typeof(RabbitMqDateTimeListConverter))]
    [UsedImplicitly]
    public List<DateTime> CertFileExpiresOn
    {
        get;
        [UsedImplicitly]
        init;
    } = [];

    /// Represents the CA certificate file associated with RabbitMQ.
    [UsedImplicitly]
    [JsonPropertyName("cacertfile")]
    public string CaCertFile
    {
        get;
        [UsedImplicitly]
        init;
    } = null!;

    /// Represents the expiration dates of the CA certificate file in the RabbitMQ API response.
    [JsonPropertyName("cacertfile_expires_on")]
    [JsonConverter(typeof(RabbitMqDateTimeListConverter))]
    [UsedImplicitly]
    public List<DateTime> CaCertFileExpiresOn
    {
        get;
        [UsedImplicitly]
        init;
    } = [];
}