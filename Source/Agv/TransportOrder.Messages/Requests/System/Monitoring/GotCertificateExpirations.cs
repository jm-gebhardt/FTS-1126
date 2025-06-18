namespace Gebhardt.Acc.TransportOrder.Messages.Requests.System.Monitoring;

/// <summary>
/// Response containing known certificate expirations.
/// </summary>
/// <param name="Certificates">List of certificate metadata</param>
public record GotCertificateExpirations(List<CertificateInfo> Certificates);