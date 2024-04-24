using System.Net;
using Newtonsoft.Json.Linq;
using JetBrains.Annotations;

namespace Gebhardt.Acc.IntegrationTest.Utils;

/// <summary>
/// TransportOrder RestApi
/// </summary>
[PublicAPI]
public class RestApi
{
    /// <summary>
    /// Default URL
    /// </summary>
    public const string DefaultApiUrl = "http://localhost/transportorder";

    private readonly string _apiUrl;

    /// <summary>
    /// Rest API constructor
    /// </summary>
    public RestApi(string url = DefaultApiUrl)
    {
        _apiUrl = url;
    }

    private async Task<T?> Request<T>(string subPath) where T : JToken
    {
        var httpClient = new HttpClient();
        var url = $"{_apiUrl}/{subPath}";

        try
        {
            var response = await httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                await WriteError($"Request '{url}' failed: {response.ReasonPhrase} ({(int)response.StatusCode})");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                return (T)JToken.Parse(content);
            }

            await WriteError($"Request '{url}' returned empty response");
            return null;
        }
        catch (HttpRequestException e)
        {
            await WriteError($"Request '{url}' failed: {e}");
            return null;
        }
    }

    private async Task<bool> Patch(string subPath)
    {
        var httpClient = new HttpClient();
        var url = $"{_apiUrl}/{subPath}";
        Console.WriteLine($"Patch({url})");
        var response = await httpClient.PatchAsync(url, null);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            await WriteError($"Request '{url}' failed ({(int)response.StatusCode}): {response.ReasonPhrase}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get all agvs.
    /// </summary>
    /// <returns>JArray of agvs or null if request is unsuccessful.</returns>
    public async Task<JArray?> GetAgvs()
    {
        return await Request<JArray>("agvs");
    }

    /// <summary>
    /// Get agv data.
    /// </summary>
    /// <returns>JObject of AgvData or null if request is unsuccessful.</returns>
    public async Task<JObject?> GetAgvData(string? correlationId)
    {
        return await Request<JObject>($"agv/{correlationId}/data");
    }

    /// <summary>
    /// Get agv graph.
    /// </summary>
    /// <returns>JObject of AgvGraph or null if request is unsuccessful.</returns>
    public async Task<JObject?> GetAgvGraph(string? correlationId)
    {
        return await Request<JObject>($"agv/{correlationId}/graph");
    }

    /// <summary>
    /// Get all maps.
    /// </summary>
    /// <returns>JArray of maps or null if request is unsuccessful.</returns>
    public async Task<JArray?> GetMaps()
    {
        return await Request<JArray>("maps");
    }

    /// <summary>
    /// Get map details.
    /// </summary>
    /// <returns>JArray of maps or null if request is unsuccessful.</returns>
    public async Task<JObject?> GetMap(string? mapCorrelationId)
    {
        return await Request<JObject>($"map/{mapCorrelationId}");
    }

    /// <summary>
    /// Get all action groups.
    /// </summary>
    /// <returns>JArray of ActionGroup or null if request is unsuccessful.</returns>
    public async Task<JArray?> GetActionGroups()
    {
        return await Request<JArray>("actionGroups");
    }

    /// <summary>
    /// Set status of agv from Discovered to Idle.
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    public async Task<bool> SetAgvIdle(string? correlationId)
    {
        return await Patch($"agv/{correlationId}/setForceIdle");
    }

    /// <summary>
    /// Wait for a specific agv.
    /// </summary>
    /// <param name="agvSerialNumber"></param>
    /// <returns>JObject of agv or null if not found or unsuccessful.</returns>
    public async Task<JToken?> WaitForAgv(string agvSerialNumber)
    {
        return await Poll(async () =>
        {
            var agvs = await GetAgvs();
            if (agvs == null || agvs.Count == 0)
            {
                await WriteError("No agvs found");
                return null;
            }

            var agv = agvs.FirstOrDefault(token => token["serialNumber"]?.ToString() == agvSerialNumber);
            if (agv == null)
            {
                await WriteError($"AGV '{agvSerialNumber}' not found");
                return null;
            }

            WriteSuccess($"Agv '${agvSerialNumber}' found!");
            return agv;
        });
    }

    /// <summary>
    /// Wait for agv data.
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns>JObject of AgvData or null if not found or unsuccessful.</returns>
    public async Task<JObject?> WaitForAgvData(string? correlationId)
    {
        return await Poll(async () =>
        {
            var data = await GetAgvData(correlationId);
            if (data == null)
            {
                await WriteError("AgvData could not be fetched");
            }
            WriteSuccess($"AgvData found for agv '{correlationId}'!");
            return data ?? null;
        });
    }

    /// <summary>
    /// Wait for agv graph.
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns>JObject of AgvGraph or null if not found or unsuccessful.</returns>
    public async Task<JObject?> WaitForAgvGraph(string? correlationId)
    {
        return await Poll(async () =>
        {
            var agvGraph = await GetAgvGraph(correlationId);
            if (agvGraph == null)
            {
                await WriteError("AgvGraph could not be fetched");
            }
            WriteSuccess($"AgvGraph found for agv '{correlationId}'!");
            return agvGraph ?? null;
        });
    }

    /// <summary>
    /// Wait for maps.
    /// </summary>
    /// <returns>JObject of Map or null if not found or unsuccessful.</returns>
    public async Task<JArray?> WaitForMaps()
    {
        return await Poll(async () =>
        {
            var maps = await GetMaps();
            if (maps == null || maps.Count == 0)
            {
                await WriteError("No maps found");
                return null;
            }
            WriteSuccess("Maps fetched!");
            return maps;
        });
    }

    /// <summary>
    /// Wait for map for agv.
    /// </summary>
    /// <param name="mapCorrelationId"></param>
    /// <param name="agvCorrelationId"></param>
    /// <returns>JObject of Map or null if not found or unsuccessful.</returns>
    public async Task<JToken?> WaitForMap(string mapCorrelationId, string agvCorrelationId)
    {
        return await Poll(async () =>
        {
            var map = await GetMap(mapCorrelationId);
            if (map == null)
            {
                await WriteError($"Map '{mapCorrelationId}' not found");
                return null;
            }

            var relatedAgvs = map["relatedAgvs"];
 
            foreach (var agv in relatedAgvs!)
            {
                if (agv.ToString() == agvCorrelationId)
                {
                    WriteSuccess($"Map '{mapCorrelationId}' fetched for agv '{agvCorrelationId}'!");
                    return map;
                }
            }
 
            await WriteError($"Map {mapCorrelationId} not found for agv {agvCorrelationId}");
            return null;
        });
    }

    /// <summary>
    /// Wait for finished action group for order.
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="longJob">Indicates that waiting can take a long time. Currently max. 10 min for long job. Default 30s for not long job [false] </param>
    /// <returns>JObject of Order or null if not found or unsuccessful.</returns>
    public async Task<JToken?> WaitForFinishedActionGroupForOrder(string orderId, bool longJob = false)
    {
        return await Poll(async () =>
        {
            var actionGroups = await GetActionGroups();
            if (actionGroups == null || actionGroups.Count == 0)
            {
                await WriteError("No action groups for order found");
                return null;
            }

            var actionGroup = actionGroups.FirstOrDefault(token => token["orderId"]?.ToString() == orderId);

            if (actionGroup == null || actionGroup["completedDate"]?.ToString() == string.Empty)
            {
                await WriteError($"Completed action group for '{orderId}' not found. Response: {actionGroups}");
                return null;
            }

            WriteSuccess($"Action group {actionGroup} for '{orderId}' fetched!");
            return actionGroup;
        }, longJob ? 600000 : 120000);
    }

    /// <summary>
    /// Poll until request is successful.
    /// </summary>
    /// <param name="pollJob"></param>
    /// <param name="timeout"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>return request result or null of unsuccessful.</returns>
    private static async Task<T?> Poll<T>(Func<Task<T?>> pollJob, int timeout = 120000)
    {
        var killTime = DateTime.Now + TimeSpan.FromMilliseconds(timeout);
        while (killTime > DateTime.Now)
        {
            try
            {
                var result = await pollJob.Invoke();
                if (result != null)
                {
                    return result;
                }
            }
            catch (Exception e)
            {
                // Do not care
                await WriteError(e.ToString());
            }
            await Task.Delay(1000);
        }

        await WriteError("Polling time out");

        return default;
    }

    private static async Task WriteError(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        await Console.Error.WriteLineAsync($"{timestamp}: {message}");
    }
    private static void WriteSuccess(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        Console.WriteLine($"{timestamp}: {message}");
    }

}