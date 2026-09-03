using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.RabbitMQ.Api;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System.Monitoring;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles certificate expiration check via RabbitMQ management API.
/// </summary>
[UsedImplicitly]
public class CertificateExpirationRequestHandler : IConsumer<GetCertificateExpirations>
{
    private readonly MassTransitConfiguration _config;
    private readonly HttpClient _httpClient;

    /// <inheritdoc cref="CertificateExpirationRequestHandler"/>
    public CertificateExpirationRequestHandler(MassTransitConfiguration config)
    {
        _config = config;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{config.Schema}://{config.Host}:{config.ManagementApiPort}")
        };

        byte[] byteArray = Encoding.ASCII.GetBytes($"{config.User}:{config.GetResolvedPassword()}");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetCertificateExpirations> context)
    {
        using var response = await _httpClient.GetAsync(_config.ExpirationCheckPath, context.CancellationToken);

        // According to the RabbitMQ management API documentation, a:
        // - 200 OK is expected if no certificates are about to expire in the requested time frame. No certificate exists or all are valid for more than 1000 years.
        // - 503 Service Unavailable is expected if certificates are about to expire in the requested time frame. Which is 1000 years in this case.
        if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.ServiceUnavailable)
        {
            string content = await response.Content.ReadAsStringAsync(context.CancellationToken);
            throw new InvalidOperationException($"RabbitMQ API error ({response.StatusCode}): {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<RabbitMqExpirationResponse>();

        if (data == null)
        {
            await context.RespondAsync(new GotCertificateExpirations([]));
            return;
        }

        var certs = new List<CertificateInfo>();

        foreach (var entry in data.Expired)
        {
            foreach (var date in entry.CertFileExpiresOn)
            {
                certs.Add(new CertificateInfo(entry.CertFile, "cert", date));
            }

            foreach (var date in entry.CaCertFileExpiresOn)
            {
                certs.Add(new CertificateInfo(entry.CaCertFile, "cacert", date));
            }
        }

        await context.RespondAsync(new GotCertificateExpirations(certs.Distinct().ToList()));
    }
}