using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services
{
    /// <summary>
    /// Test for WorkerHostOrderSim
    /// </summary>
    [TestClass]
    public class SimulationHostOrderServiceTest
    {
        /// <summary>
        /// Test if WorkerHostOrderSim can publish simulated HostOrders
        /// </summary>
        [TestMethod]
        public async Task SimulatedPublishHostOrder()
        {
            var fixture = new PollableServiceTestFixture<SimulationHostOrderService>(services =>
            {
                services.AddSingleton(_ => new SimulateOptions()
                {
                    Interval = 60,
                    Sources = new[] { "S01", "S02", "S03" },
                    Destinations = new[] { "D01", "D02", "D03" }
                });
            });
            await fixture.Pollable.Poll(It.IsAny<CancellationToken>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IHandleHostOrder>());
        }
    }
}