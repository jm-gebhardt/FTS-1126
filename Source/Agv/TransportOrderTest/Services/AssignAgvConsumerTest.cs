using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using JetBrains.Annotations;
using MassTransit;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    [UsedImplicitly]
    internal class AgvReservationSucceededConsumer : IConsumer<ReserveAgv>
    {
        public async Task Consume(ConsumeContext<ReserveAgv> context)
        {
            await context.RespondAsync(new AgvReservationSucceeded(context.Message.CorrelationId));
        }
    }
    
    [UsedImplicitly]
    internal class AgvReservationRejectedConsumer : IConsumer<ReserveAgv>
    {
        public async Task Consume(ConsumeContext<ReserveAgv> context)
        {
            await context.RespondAsync(new AgvReservationRejected(context.Message.CorrelationId));
        }
    }
    
    /// <summary>
    /// Test for handling Handle IAssignAgv messages
    /// </summary>
    [TestClass]
    public class AssignAgvConsumerTest
    {
        /// <summary>
        /// Receive IAssignAgv message when no Idle AGV is available
        /// </summary>
        [TestMethod]
        public async Task When_AgvRegistryIsEmpty_IAgvAssignFailed_isPublished()
        {
            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetIdleAgvs()).Returns(new List<IAgv>());
            var fixture = new ConsumerTestFixture<AssignAgvConsumer>( 
                services: s => { s.AddTransient(_ => mockRegistry.Object); }
                );

            string orderId = "0001";
            var correlationId = orderId.ToGuid();
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(new
            {
                CorrelationId = correlationId,
                Order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
            });

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await fixture.TestHarness.Published.Any<IAgvAssignmentSucceeded>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvAssignmentFailed>());
        }

        /// <summary>
        /// Receive AssignAgv message when no Idle AGV is available
        /// </summary>
        [TestMethod]
        public async Task When_NoIdleAgvIsIdle_AgvAssignFailedisPublished()
        {
            const string agvId = "AGV001";
            var agvCorrelationId = agvId.ToGuid();
            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetIdleAgvs()).Returns(new List<IAgv>
            {
                new AgvSaga(agvCorrelationId, agvId, nameof(AgvStateMachine.Idle))
            });
      
            var fixture = new ConsumerTestFixture<AssignAgvConsumer>( 
                services: s => { s.AddSingleton((_) => mockRegistry.Object); },
                busRegistrationConfigurator: x =>
                {
                    x.AddRequestClient<ReserveAgv>();
                    x.AddConsumer<AgvReservationRejectedConsumer>();
                }
            );
        
            const string orderId = "0001";
            var correlationId = orderId.ToGuid();
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(new
            {
                CorrelationId = correlationId,
                Order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
            });
        
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());
        
            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await fixture.TestHarness.Published.Any<IAgvAssignmentSucceeded>());
            
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvAssignmentFailed>());
        }
        
        /// <summary>
        /// Receive IAssignAgv message and assign order to Idle AGV
        /// </summary>
        [TestMethod]
        public async Task When_IdleAgvIsAvailable_AgvAssignSucceededIsPublished()
        {
            const string agvId = "AGV001";
            var agvCorrelationId = agvId.ToGuid();
            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetIdleAgvs()).Returns(new List<IAgv>
            {
                new AgvSaga(agvCorrelationId, agvId, nameof(AgvStateMachine.Idle))
            });
          
            var fixture = new ConsumerTestFixture<AssignAgvConsumer>( 
                services: s => { s.AddSingleton((_) => mockRegistry.Object); },
                busRegistrationConfigurator: x =>
                {
                    x.AddRequestClient<ReserveAgv>();
                    x.AddConsumer<AgvReservationSucceededConsumer>();
                }
            );
            
            const string orderId = "0001";
            var correlationId = orderId.ToGuid();
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(new
            {
                CorrelationId = correlationId,
                Order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
            });
        
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());
        
            var message = fixture.TestHarness.Published.Select<IAgvAssignmentSucceeded>(s => s.Context.CorrelationId == correlationId).First().Context.Message;
            Assert.AreEqual(agvId, message.AgvId);
        }
    }
}