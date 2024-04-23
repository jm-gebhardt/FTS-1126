using Gebhardt.Acc.IntegrationTest.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace Gebhardt.Acc.IntegrationTest.Agv
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
            [UsedImplicitly] 
            Cancelled,

            /// <summary>
            /// Cancellation for transport order failed
            /// </summary>
            [UsedImplicitly] 
            CancelFailed,

            /// <summary>
            /// Transport order updated
            /// </summary>
            [UsedImplicitly] 
            Updated,

            /// <summary>
            /// Transport order update failed
            /// </summary>
            [UsedImplicitly] 
            UpdateFailed,

            /// <summary>
            /// Transport order Failed
            /// </summary>
            [UsedImplicitly] 
            Failed
        }

        /// <summary>
        /// Agv Id
        /// </summary>
        public readonly string SerialNumber;

        private readonly MqttClient _mqttClient;
        private const int PublishStatusInterval = 1000;

        /// <summary>
        /// Test Agv constructor
        /// </summary>
        /// <param name="serialNumber"></param>
        public TestAgv(string serialNumber)
        {
            SerialNumber = serialNumber;
            _mqttClient = new MqttClient();
        }

        /// <summary>
        /// Start sending <see cref="MqttClient.Topic.Status"/> Messages every second 
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task StartSendingStatus(CancellationToken cancellationToken)
        {
            var message = _mqttClient.ReadSample("status.json");
            Console.WriteLine("Message");
            Console.WriteLine(message);
            while (cancellationToken is { IsCancellationRequested: false })
            {
                Console.WriteLine("Publishing Status");
                ChangeAgvSerialNumber(message);
                await _mqttClient.Publish(MqttClient.Topic.Status, message.ToString(), cancellationToken);
                await Task.Delay(PublishStatusInterval, cancellationToken);
            }
        }

        /// <summary>
        /// Subscribe to all topics
        /// </summary>
        public async Task SubscribeDataTopics()
        {
            await _mqttClient.Start();
            _mqttClient.SubscribeAndRespond(MqttClient.Topic.RequestData, MqttClient.Topic.AgvData, "AgvData.json",
                IsCurrentAgv, ChangeAgvSerialNumber);
            _mqttClient.SubscribeAndRespond(MqttClient.Topic.RequestGraph, MqttClient.Topic.Graph, "Graph.json",
                IsCurrentAgv, ChangeAgvSerialNumber);
            _mqttClient.SubscribeAndRespond(MqttClient.Topic.RequestStationList, MqttClient.Topic.StationList,
                "StationList.json", IsCurrentAgv, ChangeAgvSerialNumber);
            _mqttClient.SubscribeAndRespond(MqttClient.Topic.RequestMap, MqttClient.Topic.Map, "Map.json", IsCurrentAgv,
                ChangeAgvSerialNumber);
        }

        /// <summary>
        /// Complete order Successfully
        /// </summary>
        public async Task SubscribeToCompleteOrdersSuccessfully()
        {
            await _mqttClient.Start();
            var response = _mqttClient.ReadSample("OrderEvent.json");
            ChangeAgvSerialNumber(response);

            _mqttClient.Subscribe(MqttClient.Topic.RequestOrder, async (request) =>
            {
                if (!IsCurrentAgv(request))
                {
                    return;
                }

                response["OrderId"] = request["orderId"];

                // Pick
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
            return jsonMessage["serialNumber"]?.ToString() == SerialNumber;
        }

        private void ChangeAgvSerialNumber(JToken response)
        {
            response["serialNumber"] = SerialNumber;
        }
    }
}