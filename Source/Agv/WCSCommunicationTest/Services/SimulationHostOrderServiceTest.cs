using Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services;

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
        var fixture = new SimulationHostOrderFixture();
        await fixture.Pollable.Poll(It.IsAny<CancellationToken>());
        (await fixture.TestHarness.Published.Any<CreateHostOrder>()).ShouldBeTrue();
    }
}