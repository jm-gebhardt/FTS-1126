using System.Net;
using Newtonsoft.Json.Linq;
using JetBrains.Annotations;
using Azure;

namespace Gebhardt.StoreWare.Agv.IntegrationTest.Utils
{
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

        private string _apiUrl;

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
            catch (Exception e)
            {
                await WriteError($"Exception: {e}");
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
        /// Get all Agvs
        /// </summary>
        /// <returns>JArray of Agvs or null if request is unsuccessful</returns>
        public async Task<JArray?> GetAgvs()
        {
            return await Request<JArray>("agvs");
        }

        /// <summary>
        /// Get Agv Data
        /// </summary>
        /// <returns>JObject of AgvData or null if request is unsuccessful</returns>
        public async Task<JObject?> GetAgvData(string? correlationId)
        {
            return await Request<JObject>($"agvs/{correlationId}/data");
        }

        /// <summary>
        /// Get Agv Graph
        /// </summary>
        /// <returns>JObject of AgvGraph or null if request is unsuccessful</returns>
        public async Task<JObject?> GetAgvGraph(string? correlationId)
        {
            return await Request<JObject>($"agvs/{correlationId}/graph");
        }

        /// <summary>
        /// Get all Maps
        /// </summary>
        /// <returns>JArray of maps or null if request is unsuccessful</returns>
        public async Task<JArray?> GetMaps()
        {
            return await Request<JArray>("maps");
        }

        /// <summary>
        /// Get all Maps
        /// </summary>
        /// <returns>JArray of Orders or null if request is unsuccessful</returns>
        public async Task<JArray?> GetOrders()
        {
            return await Request<JArray>("orders");
        }
        
        /// <summary>
        /// Get all Maps
        /// </summary>
        /// <returns>JArray of Orders or null if request is unsuccessful</returns>
        public async Task<JArray?> GetActionsGroup()
        {
            return await Request<JArray>("actionGroups?limit=1");
        }

        /// <summary>
        /// Set status of agv from Discovered to Idle
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public async Task<bool> UnlockAgv(string? correlationId)
        {
            return await Patch($"agvs/{correlationId}/unlock");
        }

        /// <summary>
        /// Wait for a specific AGV
        /// </summary>
        /// <param name="agvId"></param>
        /// <returns>JObject of AGV or null if not found or unsuccessful</returns>
        public async Task<JToken?> WaitForAgv(string agvId)
        {
            return await Poll(async () =>
            {
                var agvs = await GetAgvs();
                if (agvs == null || agvs.Count == 0)
                {
                    await WriteError($"No agvs found");
                    return null;
                }

                var agv = agvs.FirstOrDefault(token => token["agvId"]?.ToString() == agvId);
                if (agv == null)
                {
                    await WriteError($"AGV '{agvId}' not found");
                    return null;
                }

                return agv;
            });
        }

        /// <summary>
        /// Wait for AgvData
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns>JObject of AgvData or null if not found or unsuccessful</returns>
        public async Task<JObject?> WaitForAgvData(string? correlationId)
        {
            return await Poll(async () =>
            {
                var data = await GetAgvData(correlationId);
                if (data == null)
                {
                    await WriteError($"AgvData could not be fetched");
                }
                return data ?? null;
            });
        }

        /// <summary>
        /// Wait for Agv Graph
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns>JObject of AgvGraph or null if not found or unsuccessful</returns>
        public async Task<JObject?> WaitForAgvGraph(string? correlationId)
        {
            return await Poll(async () =>
            {
                var data = await GetAgvGraph(correlationId);
                if (data == null)
                {
                    await WriteError($"AgvGraph could not be fetched");
                }
                return data ?? null;
            });
        }

        /// <summary>
        /// Wait for Agv Graph
        /// </summary>
        /// <param name="agvCorrelationId"></param>
        /// <returns>JObject of Map or null if not found or unsuccessful</returns>
        public async Task<JToken?> WaitForMap(string? agvCorrelationId)
        {
            return await Poll(async () =>
            {
                var maps = await GetMaps();
                if (maps == null || maps.Count == 0)
                {
                    await WriteError($"No maps found");
                    return null;
                }

                foreach (var map in maps)
                {
                    var relatedAgvs = map["relatedAgvs"];
                    if (relatedAgvs == null) continue;

                    foreach (var correlationId in relatedAgvs)
                    {
                        if (correlationId == null) continue;
                        if (correlationId.ToString() == agvCorrelationId)
                        {
                            return map;
                        }
                    }
                }

                await WriteError($"Map not found. Response: {maps}");
                return null;
            });
        }

        /// <summary>
        /// Wait for Order
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="orderState"></param>
        /// <param name="longJob">Indicates that waiting can take a long time. Currently max. 20 min for long job. Default 60s for not long job [false] </param>
        /// <returns>JObject of Order or null if not found or unsuccessful</returns>
        public async Task<JToken?> WaitForOrder(string orderId, string? orderState = null, bool longJob = false)
        {
            return await Poll(async () =>
            {
                var actions = await GetActionsGroup();
                if (actions == null || actions.Count == 0)
                {
                    await WriteError($"No actions found");
                    return null;
                }

                var order = actions.FirstOrDefault(token => token?["orderId"]?.ToString() == orderId);
                if (order == null)
                {
                    await WriteError($"Order '{orderId}' not found. Response: {actions}");
                    return null;
                }

                var state = order["state"]?.ToString();
                if (orderState != null && orderState != state)
                {
                    await WriteError($"Order '{orderId}' does not have the correct state (expected: {orderState}, actual: {state})");
                    return null;
                }

                return order;
            }, longJob ? 600000 : 120000);
        }

        /// <summary>
        /// Poll until request is successful
        /// </summary>
        /// <param name="pollJob"></param>
        /// <param name="timeout"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>return request result or null of unsuccessful</returns>
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
    }
}