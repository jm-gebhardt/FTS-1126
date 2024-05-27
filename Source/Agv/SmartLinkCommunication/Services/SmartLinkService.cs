using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.SmartLinkCommunication.Models;
using RestSharp;

namespace Gebhardt.Acc.SmartLinkCommunication.Services;

/// <summary>
/// Service that requests data from SmartLink Rest API
/// </summary>
public class SmartLinkService : ISmartLinkService
{
    private readonly IRestClient _restClient;
    private readonly IModuleConfigProvider _moduleConfigProvider;

    /// <inheritdoc cref="SmartLinkService"/>
    /// <param name="restClient"></param>
    /// <param name="moduleConfigProvider"></param>
    public SmartLinkService(IRestClient restClient, IModuleConfigProvider moduleConfigProvider)
    {
        _restClient = restClient;
        _moduleConfigProvider = moduleConfigProvider;
    }

    /// <inheritdoc />
    /// <exception cref="RequestFailedException"></exception>
    public async Task<SmartLinkData> GetDataAsync()
    {
        var request = new RestRequest(new Uri(_moduleConfigProvider.ModuleConfig.SmartLinkUrl))
        {
            Timeout = TimeSpan.FromSeconds(_moduleConfigProvider.ModuleConfig.RequestTimeout)
        };
        var response = await _restClient.GetAsync<SmartLinkData>(request);

        if (response == null)
        {
            throw new RequestFailedException("Empty response from SmartLink");
        }

        return response;
    }
}