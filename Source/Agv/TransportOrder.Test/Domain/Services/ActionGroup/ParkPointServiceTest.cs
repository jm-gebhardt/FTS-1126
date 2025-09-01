using Gebhardt.Acc.Common.Exceptions;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

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