using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation
{
    /// <summary>
    /// SimulatedAgvProviderTest
    /// </summary>
    [TestClass]
    public class SimulatedAgvProviderTest
    {
        /// <summary>
        /// Test SingleAgv Single Source And Destination CreateAgv Published Successful
        /// </summary>
        [TestMethod]
        public void SingleAgv_SingleSourceAndDestination_CreateAgvPublished_Successful()
        {
            var options = new SimulateOptions()
            {
                Agvs = 1,
                Sources = new[] {"S1"},
                Destinations = new []{"D1"},
                GridRows = 3
            };

            var bus = new Mock<IBus>();
            var createAgvMessages = new List<SimulatedAgvStateMachine.CreateAgv>();
            bus.Setup(s => s.Publish(Capture.In(createAgvMessages), default));
            var provider = new SimulatedAgvProvider(options, bus.Object);
            provider.Create();

            bus.Verify(s => s.Publish(It.IsAny<SimulatedAgvStateMachine.CreateAgv>(), default), Times.Once);

            var firstAgv = createAgvMessages.First();
            Assert.AreNotEqual(Guid.Empty,firstAgv.Agv.CorrelationId);
            Assert.IsNotNull("",firstAgv.Agv.AgvId);
            Assert.IsNotNull(firstAgv.Agv.AgvGraph);
            Assert.IsNotNull(firstAgv.Agv.AgvData);
        }
        
        /// <summary>
        /// Test TwoAgv Single Source And Destination CreateAgv Published with unique AgvId Successful
        /// </summary>
        [TestMethod]
        public void TwoAgvs_SingleSourceAndDestination_CreateAgvPublished_AgvIdUnique_Successful()
        {
            var options = new SimulateOptions()
            {
                Agvs = 2,
                Sources = new[] {"S1"},
                Destinations = new []{"D1"},
                GridRows = 3
            };

            var bus = new Mock<IBus>();
            var createAgvMessages = new List<SimulatedAgvStateMachine.CreateAgv>();
            bus.Setup(s => s.Publish(Capture.In(createAgvMessages), default));
            var provider = new SimulatedAgvProvider(options, bus.Object);
            provider.Create();
            
            bus.Verify(s => s.Publish(It.IsAny<SimulatedAgvStateMachine.CreateAgv>(), default), Times.Exactly(2));

            var firstAgv = createAgvMessages.First();
            Assert.AreNotEqual(Guid.Empty,firstAgv.Agv.CorrelationId);
            Assert.IsNotNull("",firstAgv.Agv.AgvId);
            Assert.IsNotNull(firstAgv.Agv.AgvGraph);
            Assert.IsNotNull(firstAgv.Agv.AgvData);
            
            var secondAgv = createAgvMessages[1];
            Assert.AreNotEqual(Guid.Empty,secondAgv.Agv.CorrelationId);
            Assert.IsNotNull("",secondAgv.Agv.AgvId);
            Assert.IsNotNull(secondAgv.Agv.AgvGraph);
            Assert.IsNotNull(secondAgv.Agv.AgvData);
            
            Assert.AreNotEqual(firstAgv.Agv.AgvId, secondAgv.Agv.AgvId);
            Assert.AreNotEqual(firstAgv.Agv.CorrelationId, secondAgv.Agv.CorrelationId);
        }
    }
}


