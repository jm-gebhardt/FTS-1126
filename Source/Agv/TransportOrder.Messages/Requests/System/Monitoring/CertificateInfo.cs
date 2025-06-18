namespace Gebhardt.Acc.TransportOrder.Messages.Requests.System.Monitoring;

/// <summary>
/// Represents a single certificate's expiration metadata.
/// </summary>
/// <param name="File">Path to the certificate file</param>
/// <param name="Type">Type of certificate (e.g. "cert", "cacert")</param>
/// <param name="ExpirationDate">UTC expiration date</param>
public record CertificateInfo(string File, string Type, DateTime ExpirationDate);