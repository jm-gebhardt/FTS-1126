using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.SmartLinkCommunication.Models;
using RestSharp;
using System.Text.Json;

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
        var response = await _restClient.ExecuteAsync(request);
        if (response == null || !response.IsSuccessful || response.Content == null)
        {
            throw new RequestFailedException("Empty response from SmartLink");
        }
        var data = JsonSerializer.Deserialize<Dictionary<string, PinStatus>>(response.Content);
        if (data == null)
        {
            throw new RequestFailedException("Empty response from SmartLink");
        }
        var smartLinkData = new SmartLinkData(data);
        return smartLinkData;
    }
}