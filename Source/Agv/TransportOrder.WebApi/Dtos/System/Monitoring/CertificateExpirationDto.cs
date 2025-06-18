namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.System.Monitoring;

/// <summary>
/// Represents data related to the expiration of a certificate.
/// </summary>
/// <param name="File">Path to the certificate file</param>
/// <param name="Type">Type of certificate (e.g. "cert", "cacert")</param>
/// <param name="ExpirationDate">UTC expiration date</param>
public record CertificateExpirationDto(string File, string Type, DateTime ExpirationDate);