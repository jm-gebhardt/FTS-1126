using Gebhardt.StoreWare.Agv.IntegrationTest.Utils;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;

namespace Gebhardt.StoreWare.Agv.IntegrationTest
{
    /// <summary>
    /// Test AGV Implementation
    /// </summary>
    public class TestAgv
    {
        /// <summary>
        /// Order Event Types
        /// </summary>
        private enum OrderEventType
        {
            /// <summary>
            /// Transport order started
            /// </summary>
            Started,
            /// <summary>
            /// Pick for transport order completed
            /// </summary>
            Picked,
            /// <summary>
            /// Transport order completed
            /// </summary>
            Completed,
            /// <summary>
            /// Transport order cancelled
            /// </summary>
            Cancelled,
            /// <summary>
            /// Cancellation for transport order failed
            /// </summary>
            CancelFailed,
            /// <summary>
            /// Transport order updated
            /// </summary>
            Updated,
            /// <summary>
            /// Transport order update failed
            /// </summary>
            UpdateFailed,
            /// <summary>
            /// Transport order Failed
            /// </summary>
            Failed
        }
        
        /// <summary>
        /// Agv Id
        /// </summary>
        public readonly string AgvId;
        private readonly MqttClient _mqttClient;
        private readonly RestApi _restApi;
        private CancellationToken? _sendAgvStatusCancellationToken = null;
        private const int PublishStatusIntervall = 1000;

        /// <summary>
        /// Test Agv Contructor
        /// </summary>
        /// <param name="agvId"></param>
        public TestAgv(string agvId)
        {
            AgvId = agvId;
            _mqttClient = new MqttClient();
            _restApi = new RestApi();
        }

        /// <summary>
        /// Start sending <see cref="MqttClient.Topic.Status"/> Messages every second 
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task StartSendingStatus(CancellationToken cancellationToken)
        {
            
            var message = _mqttClient.ReadSample("status.json");
            _sendAgvStatusCancellationToken = cancellationToken;
            Console.WriteLine("Message");
            Console.WriteLine(message);
            while (cancellationToken is {IsCancellationRequested: false})
            {
                Console.WriteLine("Publishing Status");
                ChangeAgvId(message);
                await _mqttClient.Publish(MqttClient.Topic.Status, message.ToString(), cancellationToken);
                await Task.Delay(PublishStatusIntervall, cancellationToken);
            }

            _sendAgvStatusCancellationToken = null;
        }

        /// <summary>
        /// Subscribe to all topics
        /// </summary>
        public async Task SubscribeDataTopics()
        {
            await _mqttClient.Start();
            _mqttClient.SubscribeAndRespond(MqttClient.Topic.RequestData, MqttClient.Topic.AgvData, "AgvData.json", IsCurrentAgv, ChangeAgvId);
            _mqttClient.SubscribeAndRespond(MqttClient.Topic.RequestGraph, MqttClient.Topic.Graph, "Graph.json", IsCurrentAgv, ChangeAgvId);
            _mqttClient.SubscribeAndRespond(MqttClient.Topic.RequestStationList, MqttClient.Topic.StationList, "StationList.json", IsCurrentAgv, ChangeAgvId);
            _mqttClient.SubscribeAndRespond(MqttClient.Topic.RequestMap, MqttClient.Topic.Map, "Map.json", IsCurrentAgv, ChangeAgvId);
        }

        /// <summary>
        /// Complete order Successfully
        /// </summary>
        public async Task SubscribeToCompleteOrdersSuccessfully()
        {
            await _mqttClient.Start();
            var response =  _mqttClient.ReadSample("OrderEvent.json");
            ChangeAgvId(response);
            
            _mqttClient.Subscribe(MqttClient.Topic.RequestOrder, async (request) =>
            {
                if (!IsCurrentAgv(request))
                {
                    return;
                }

                response["OrderId"] = request["orderId"];
                
                response["OrderEventType"] = OrderEventType.Started.ToString();
                await _mqttClient.Publish(MqttClient.Topic.OrderEvent, response.ToString());
                await Task.Delay(1000);
                
                response["OrderEventType"] = OrderEventType.Picked.ToString();
                await _mqttClient.Publish(MqttClient.Topic.OrderEvent, response.ToString());
                await Task.Delay(1000);
                
                response["OrderEventType"] = OrderEventType.Completed.ToString();
                await _mqttClient.Publish(MqttClient.Topic.OrderEvent, response.ToString());
            });
            
        }

        private bool IsCurrentAgv(JToken jsonMessage)
        {
            return jsonMessage["serialNumber"].ToString() == AgvId;
        }

        private void ChangeAgvId(JToken response)
        {
            response["serialNumber"] = AgvId;
        }
    }
}