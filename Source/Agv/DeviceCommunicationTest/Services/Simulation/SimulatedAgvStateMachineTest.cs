using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation
{
    /// <summary>
    /// SimulatedAgvStateMachine Test
    /// </summary>
    [TestClass]
    public class SimulatedAgvStateMachineTest
    {
        private record Agv(Guid CorrelationId,string AgvId, string CurrentState, AgvStatus? AgvStatus, AgvGraph? AgvGraph, AgvData? AgvData) : IAgv;
        
        /// <summary>
        /// InitialCreation_SingleAgv_Successful
        /// </summary>
        [TestMethod]
        public async Task InitialCreation_SingleAgv_Successful()
        {
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>();
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();
            var graph = new GridGraphGenerator(new[] {"S1"}, new[] {"D1"}).Generate(correlationId);
            var data = new AgvData(correlationId, "model", "type",null, 10, 1, 1, 1,1,1, 1, 1, Array.Empty<AgvData.Sensor>());
            var agv = new Agv(correlationId, agvId , "", null, graph, data);
            await fixture.TestHarness.Bus.Publish(new SimulatedAgvStateMachine.CreateAgv(correlationId, agv));
            await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>();
            
            var createdSaga = fixture.SagaHarness.Sagas.Select(c => c.CorrelationId == correlationId).First().Saga;
            
            Assert.AreEqual(correlationId, createdSaga.CorrelationId);
            Assert.AreEqual(agvId, createdSaga.AgvId);
            Assert.IsNotNull(createdSaga.AgvStatus);
            Assert.IsNotNull(createdSaga.AgvGraph);
            Assert.IsNotNull(createdSaga.AgvData);
        }
        
        /// <summary>
        /// InitialCreation_SingleAgv_Status_Correct
        /// </summary>
        [TestMethod]
        public async Task InitialCreation_SingleAgv_Status_Correct()
        {
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                busRegistrationConfigurator: x => x.AddDelayedMessageScheduler(),
                inMemoryConfiguration: (_, cfg) => cfg.UseDelayedMessageScheduler()
            );
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();
            var agv = new Agv(correlationId, agvId , "", null, null, null);
            await fixture.TestHarness.Bus.Publish(new SimulatedAgvStateMachine.CreateAgv(correlationId, agv));
            
            
            var createdSaga = fixture.SagaHarness.Sagas.Select(c => c.CorrelationId == correlationId).First().Saga;
            var agvStatus = createdSaga.AgvStatus; 
            
            Assert.IsNotNull(agvStatus);
            Assert.AreEqual(correlationId, agvStatus.CorrelationId);
            Assert.IsNull(agvStatus.OrderId);
            Assert.IsFalse(agvStatus.Busy);
            Assert.IsFalse(agvStatus.Automatic);
            Assert.IsFalse(agvStatus.Charging);
            Assert.IsFalse(agvStatus.Disturbed);
            Assert.IsNull(agvStatus.LastNodeId);
        }

        /// <summary>
        /// When_RequestAgvGraph_AgvGraphReceived_isPublished
        /// </summary>
        [TestMethod]
        public async Task When_RequestAgvGraph_AgvGraphReceived_isPublished()
        {
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>();
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();
            
            var graph = new GridGraphGenerator(new[] {"S1"}, new[] {"D1"}).Generate(correlationId);
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId , currentState: nameof(SimulatedAgvStateMachine.Idle), agvGraph: graph);
            fixture.Add(agvSaga);

            await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, agvId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>());
            
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvGraphReceived>());
            var message = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(agvId, message.AgvId);
            Assert.AreEqual(graph, message.Graph);
        }
        
        /// <summary>
        /// When_RequestAgvGraph_AgvGraph_Null_NoMessageIsPublished
        /// </summary>
        [TestMethod]
        public async Task When_RequestAgvGraph_AgvGraphIsNull_NoMessageIsPublished()
        {
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>();
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();
            
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId , currentState: nameof(SimulatedAgvStateMachine.Idle));
            fixture.Add(agvSaga);

            await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, agvId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>());
            var token = new CancellationTokenSource();
            token.CancelAfter(10);
            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvGraphReceived>(token.Token));
        }
        
        /// <summary>
        /// When_RequestAgvData_AgvDataIsNull_NoMessageIsPublished
        /// </summary>
        [TestMethod]
        public async Task When_RequestAgvData_AgvDataIsNull_NoMessageIsPublished()
        {
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>();
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();
            
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId , currentState: nameof(SimulatedAgvStateMachine.Idle));
            fixture.Add(agvSaga);

            await fixture.TestHarness.Bus.Publish(new RequestAgvData(correlationId, agvId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvData>());
            var token = new CancellationTokenSource();
            token.CancelAfter(10);
            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvDataReceived>(token.Token));
        }
        
        /// <summary>
        /// When_RequestAgvGraph_AgvGraphReceived_isPublished
        /// </summary>
        [TestMethod]
        public async Task When_RequestAgvData_AgvDataReceived_isPublished()
        {
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>();
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();
           
            var data = new AgvData(correlationId, "model", "type",null, 10, 1, 1, 1,1,1, 1, 1, Array.Empty<AgvData.Sensor>());
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId , currentState: nameof(SimulatedAgvStateMachine.Idle), agvData: data);
            fixture.Add(agvSaga);

            await fixture.TestHarness.Bus.Publish(new RequestAgvData(correlationId, agvId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvData>());
            
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvDataReceived>());
            var message = fixture.TestHarness.Published.Select<AgvDataReceived>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(agvId, message.AgvId);
            Assert.AreEqual(data, message.Data);
        }

        /// <summary>
        /// When_ExecuteTransportOrder_TransportOrderStarted_IsPublished_TransportOrderCompleted_Is_Published
        /// </summary>
        [TestMethod]
        public async Task When_ExecuteTransportOrder_TransportOrderStarted_IsPublished_TransportOrderCompleted_Is_Published()
        {
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                busRegistrationConfigurator: x => x.AddDelayedMessageScheduler(),
                inMemoryConfiguration: (_, cfg) => cfg.UseDelayedMessageScheduler()
                );
            const string agvId = "Agv#1";
            var agvCorrelationId = agvId.ToGuid();
            const string orderId = "Order#1";
            var orderCorrelationId = orderId.ToGuid();
            var order = new OrderDetails(orderCorrelationId, orderId, "S1", "D1", "123", HostOrderCreator.Simulation);
            var graph = new GridGraphGenerator(new[] {order.Source}, new[] {order.Destination}, gridLength: 1).Generate(agvCorrelationId);
            string[] path = {
                order.Source, 
                graph.Nodes.Single(n => n.Type == AgvGraph.NodeType.WayPoint).Id, 
                order.Destination
            };
            var data = new AgvData(agvCorrelationId, "model", "type", null, 10, 1, 1, 1, 1, 1, 1, 1, Array.Empty<AgvData.Sensor>());
            var agvSaga = new SimulatedAgvSaga(agvCorrelationId, agvId, currentState: nameof(SimulatedAgvStateMachine.Idle), agvData: data)
            {
                NodeTravelTime = 0.1
            };
            fixture.Add(agvSaga);

            Assert.IsFalse(agvSaga.AgvStatus!.Busy);
            Assert.IsNull(agvSaga.LastNodeId);
            
            // Start Order
            await fixture.TestHarness.Bus.Publish(new ExecuteTransportOrder(agvCorrelationId, order, agvId, path));
            
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<ExecuteTransportOrder>());      
            Assert.IsTrue(await fixture.TestHarness.Published.Any<TransportOrderStarted>());
            var startedMessage = fixture.TestHarness.Published.Select<TransportOrderStarted>().First().Context.Message;
            Assert.AreEqual(orderCorrelationId, startedMessage.CorrelationId);
            
            Assert.IsTrue(agvSaga.AgvStatus!.Busy);
            Assert.IsNotNull(agvSaga.TimeToFinish);
            Assert.IsNotNull(agvSaga.OrderStartDate);
            Assert.IsNotNull(agvSaga.OrderFinishDate);
            
            // wait until order is picked up
            if (agvSaga.TimeToPick!.Value.TotalSeconds > 0)
            {
                Thread.Sleep(agvSaga.TimeToPick!.Value);
            }

            // Order picked up
            Assert.IsTrue(await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.SchedulePickOrder>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.SchedulePickOrder>());
            
            Assert.IsTrue(await fixture.TestHarness.Published.Any<TransportOrderPickCompleted>());
            var pickedMessage = fixture.TestHarness.Published.Select<TransportOrderPickCompleted>().First().Context.Message;
            Assert.AreEqual(order.CorrelationId, pickedMessage.CorrelationId);
            
            // wait until order is completed
            Thread.Sleep(agvSaga.TimeToFinish!.Value);
            
            // Order completed
            Assert.IsTrue(await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleFinishOrder>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleFinishOrder>());
            
            Assert.IsFalse(agvSaga.AgvStatus!.Busy);
            Assert.IsNull(agvSaga.TimeToFinish);
            Assert.IsNull(agvSaga.OrderStartDate);
            Assert.IsNull(agvSaga.OrderFinishDate);
            Assert.AreEqual(order.Destination, agvSaga.LastNodeId);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<TransportOrderCompleted>());
            var completedMessage = fixture.TestHarness.Published.Select<TransportOrderCompleted>().First().Context.Message;
            Assert.AreEqual(order.CorrelationId, completedMessage.CorrelationId);
        }
    }
}