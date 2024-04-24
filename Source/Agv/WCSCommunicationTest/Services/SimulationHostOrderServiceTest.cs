using Gebhardt.Acc.WCSCommunication.Messages.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.WCSCommunicationTest.Services;

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
        await fixture.ShouldHavePublishedAsync<CreateHostOrder>();
    }
}