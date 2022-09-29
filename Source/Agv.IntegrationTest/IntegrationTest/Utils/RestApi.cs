using Newtonsoft.Json.Linq;
using JetBrains.Annotations;

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

        private async Task<T?> Request<T>(string subPath) where T: JToken
        {
            var httpClient = new HttpClient();
            var url = $"{_apiUrl}/{subPath}";
            var response = await httpClient.GetStringAsync(url);
            if (!string.IsNullOrEmpty(response))
            {
                return (T)JToken.Parse(response);
            }

            return null;
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
        public async Task<JObject?> GetAgvData(string correlationId)
        {
            return await Request<JObject>($"agvs/{correlationId}/data");
        }
        
        /// <summary>
        /// Get Agv Graph
        /// </summary>
        /// <returns>JObject of AgvGraph or null if request is unsuccessful</returns>
        public async Task<JObject?> GetAgvGraph(string correlationId)
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
        /// Wait for a specific AGV
        /// </summary>
        /// <param name="agvId"></param>
        /// <returns>JObject of AGV or null if not found or unsuccessful</returns>
        public async Task<JToken?> WaitForAgv(string agvId)
        {
            return await Poll(async () =>
            {
                var agvs = await GetAgvs();
                if (agvs is {Count: > 0} && agvs.Any(a => a["agvId"].ToString() == agvId))
                {
                    return agvs.First(token => token["agvId"].ToString() == agvId);
                }
                return null;
            });
        }

        /// <summary>
        /// Wait for AgvData
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns>JObject of AgvData or null if not found or unsuccessful</returns>
        public async Task<JObject?> WaitForAgvData(string correlationId)
        {
            return await Poll(async () =>
            {
                var data = await GetAgvData(correlationId);
                return data ?? null;
            });
        }
        
        /// <summary>
        /// Wait for Agv Graph
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns>JObject of AgvGraph or null if not found or unsuccessful</returns>
        public async Task<JObject?> WaitForAgvGraph(string correlationId)
        {
            return await Poll(async () =>
            {
                var data = await GetAgvGraph(correlationId);
                return data ?? null;
            });
        }
        
        /// <summary>
        /// Wait for Agv Graph
        /// </summary>
        /// <param name="agvCorrelationId"></param>
        /// <returns>JObject of Map or null if not found or unsuccessful</returns>
        public async Task<JToken?> WaitForMap(string agvCorrelationId)
        {
            return await Poll(async () =>
            {
                var maps = await GetMaps();
                if (maps is {Count: > 0} && maps.Any(a => a["relatedAgvs"].Any( c => c.ToString() == agvCorrelationId)))
                {
                    return maps.First(token => token["relatedAgvs"].Any(c => c.ToString() == agvCorrelationId));
                }
                return null;
            });
        }

        /// <summary>
        /// Wait for Order
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="orderState"></param>
        /// <returns>JObject of Order or null if not found or unsuccessful</returns>
        public async Task<JToken?> WaitForOrder(string orderId, string? orderState = null )
        {
            return await Poll(async () =>
            {
                var orders = await GetOrders();
                if (orders is {Count: > 0} && 
                    orders.Any(a => a["orderDetails"]["orderId"].ToString() == orderId)
                   )
                {
                    var order = orders.First(token => token["orderDetails"]["orderId"].ToString() == orderId);
                    if (orderState == null || order["currentState"].ToString() == orderState)
                    {
                        return order;
                    }
                }
                return null;
            });
        }


        /// <summary>
        /// Poll until request is successful
        /// </summary>
        /// <param name="pollJob"></param>
        /// <param name="timeout"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>return request result or null of unsuccessful</returns>
        private static async Task<T?> Poll<T>(Func<Task<T?>> pollJob, int timeout=20000)
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
                    await Console.Error.WriteLineAsync(e.ToString());
                }
                await Task.Delay(200);
            }

            return default;
        }
    }
}