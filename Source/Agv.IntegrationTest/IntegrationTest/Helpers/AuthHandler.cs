using System.Net.Http.Headers;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.IntegrationTest.Helpers;

public class AuthHandler : DelegatingHandler
{
    private readonly string _baseUrl;
    private readonly string _keycloakClientId;
    private readonly string _userName;
    private readonly string _userPassword;
    private readonly ILogger _logger;
    private string? _token;

    public AuthHandler(string baseUrl, string keycloakClientId, string userName, string userPassword, ILogger logger)
    {
        _baseUrl = baseUrl;
        _keycloakClientId = keycloakClientId;
        _userName = userName;
        _userPassword = userPassword;
        _logger = logger;
    }

    private async Task GetAuthToken()
    {
        var httpClient = new HttpClient();
        var serverUrl = $"{_baseUrl}security/";
        const string realmUrl = "realms/agv/";
        const string discoveryUrl = $"{realmUrl}.well-known/openid-configuration";

        try
        {
            var tokenEndpoint = $"{serverUrl}{realmUrl}protocol/openid-connect/token";
            var tokenStr = await httpClient.RequestPasswordTokenAsync(
                new PasswordTokenRequest()
                {
                    Address = tokenEndpoint,
                    ClientId = _keycloakClientId,
                    UserName = _userName,
                    Password = _userPassword,
                    Scope = ""
                });

            _token = tokenStr.AccessToken;

            if (string.IsNullOrEmpty(_token))
            {
                _logger.LogError("Failed to retrieve auth token from {DiscoveryUrl}", discoveryUrl);
                throw new InvalidOperationException("Failed to retrieve auth token.");
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Error retrieving auth token from {DiscoveryUrl}", discoveryUrl);
            throw;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_token))
        {
            await GetAuthToken();
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return await base.SendAsync(request, cancellationToken);
    }
}