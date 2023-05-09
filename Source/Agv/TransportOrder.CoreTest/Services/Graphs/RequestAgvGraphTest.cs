using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Graphs
{
    /// <summary>
    /// Tests for <see cref="RequestAgvGraph"/>.
    /// </summary>
    [TestClass]
    public class RequestAgvGraphTest
    {
        private const string AgvTestIdentifier01 = "#2410#";

        /// <summary>
        /// Test building and publishing of <see cref="RequestAgvGraph"/>.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task PublishRequestAgvGraph()
        {
            var fixture = new MassTransitTestFixture();

            await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(
                correlationId: AgvTestIdentifier01.ToGuid(),
                agvId: AgvTestIdentifier01
            ));
            (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
        }
    }
}
