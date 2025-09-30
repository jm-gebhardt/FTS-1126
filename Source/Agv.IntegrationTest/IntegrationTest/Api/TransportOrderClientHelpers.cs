using Gebhardt.Acc.IntegrationTest.Helpers;
using Gebhardt.Acc.IntegrationTest.Utils;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.IntegrationTest.Api;

public partial class TransportOrderClient
{
    private const string BaseUrlPath = "http://127.0.0.1/";
    private const string DefaultApiUrl = $"{BaseUrlPath}transportorder";

    public static TransportOrderClient GetClient(EnvFile envFile, ILogger logger)
    {
        var authHandler = new AuthHandler(BaseUrlPath,
            envFile.KeycloakClientId,
            envFile.KeycloakUsername,
            envFile.KeycloakUserPassword,
            logger
        )
        {
            InnerHandler = new HttpClientHandler()
        };

        var http = new HttpClient(authHandler) { BaseAddress = new Uri(DefaultApiUrl) };

        var client = new TransportOrderClient(http);
        return client;
    }
}