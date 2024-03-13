using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

[TestClass]
public class ParkPointServiceTest
{
    [TestMethod]
    public void ReserveParkAsync_ReservationFails_Throws()
    {
        var fixture = new ParkPointServiceTestFixture()
            .ReserveNodeAsyncReturns(false);

        Should.ThrowAsync<NodeNotAvailableException>(async () => await fixture.ParkPointService.ReserveParkAsync("nodeId", Guid.NewGuid()));
    }
}