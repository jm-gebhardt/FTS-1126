using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Reservations;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.TrafficManagement;

[TestClass]
public class ReservationManagerTests
{
    [TestMethod]
    public void TryReserve_FreeResource_ReturnsTicket()
    {
        var manager = CreateManager();
        var resource = RoutingResourceGenerator.GenerateResource();
        var agvCorrelationId = Guid.NewGuid();

        bool wasReserved = manager.TryReserve(resource, agvCorrelationId, out var ticket);

        wasReserved.ShouldBeTrue();
        ticket.ShouldNotBeNull();
        ticket.TicketId.ShouldNotBe(Guid.Empty);
        ticket.RoutingResource.ShouldBeSameAs(resource);
        ticket.AgvCorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public void TryReserve_ResourceReservedByOtherAgv_ReturnsFalse()
    {
        var manager = CreateManager();
        var resource = RoutingResourceGenerator.GenerateResource();
        manager.TryReserve(resource, Guid.NewGuid(), out _).ShouldBeTrue();

        bool wasReserved = manager.TryReserve(resource, Guid.NewGuid(), out var ticket);

        wasReserved.ShouldBeFalse();
        ticket.ShouldBeNull();
    }

    [TestMethod]
    public void TryReserve_ResourceReservedBySameAgv_ReturnsDistinctTicket()
    {
        var manager = CreateManager();
        var resource = RoutingResourceGenerator.GenerateResource();
        var agvCorrelationId = Guid.NewGuid();
        manager.TryReserve(resource, agvCorrelationId, out var firstTicket).ShouldBeTrue();

        bool wasReserved = manager.TryReserve(resource, agvCorrelationId, out var secondTicket);

        wasReserved.ShouldBeTrue();
        firstTicket.ShouldNotBeNull();
        secondTicket.ShouldNotBeNull();
        secondTicket.TicketId.ShouldNotBe(firstTicket.TicketId);
    }

    [TestMethod]
    public void TryReserve_ConflictingResourceReservedByOtherAgv_ReturnsFalse()
    {
        var manager = CreateManager();
        var reservedResource = RoutingResourceGenerator.GenerateResource();
        var requestedResource = RoutingResourceGenerator.GenerateResource(reservedResource);
        manager.TryReserve(reservedResource, Guid.NewGuid(), out _).ShouldBeTrue();

        bool wasReserved = manager.TryReserve(requestedResource, Guid.NewGuid(), out var ticket);

        wasReserved.ShouldBeFalse();
        ticket.ShouldBeNull();
    }

    [TestMethod]
    public void TryReserve_MultipleAvailableResources_ReservesAllResources()
    {
        var manager = CreateManager();
        var firstResource = RoutingResourceGenerator.GenerateResource();
        var secondResource = RoutingResourceGenerator.GenerateResource();
        var agvCorrelationId = Guid.NewGuid();

        bool wasReserved = manager.TryReserve([firstResource, secondResource], agvCorrelationId, out var tickets);

        wasReserved.ShouldBeTrue();
        tickets.Count.ShouldBe(2);
        tickets.Single(ticket => ticket.RoutingResource == firstResource).RoutingResource.ShouldBeSameAs(firstResource);
        tickets.Single(ticket => ticket.RoutingResource == secondResource).RoutingResource.ShouldBeSameAs(secondResource);
        tickets.ShouldAllBe(ticket => ticket.AgvCorrelationId == agvCorrelationId);
        manager.TryReserve(firstResource, Guid.NewGuid(), out _).ShouldBeFalse();
        manager.TryReserve(secondResource, Guid.NewGuid(), out _).ShouldBeFalse();
    }

    [TestMethod]
    public void TryReserve_MultipleResourcesWithBlockedResource_DoesNotReserveAnyResource()
    {
        var manager = CreateManager();
        var availableResource = RoutingResourceGenerator.GenerateResource();
        var blockedResource = RoutingResourceGenerator.GenerateResource();
        var blockingAgvId = Guid.NewGuid();
        var requestingAgvId = Guid.NewGuid();
        manager.TryReserve(blockedResource, blockingAgvId, out _).ShouldBeTrue();

        bool wasReserved = manager.TryReserve([availableResource, blockedResource], requestingAgvId, out var tickets);

        wasReserved.ShouldBeFalse();
        tickets.ShouldBeEmpty();
        manager.TryReserve(availableResource, blockingAgvId, out _).ShouldBeTrue();
    }

    [TestMethod]
    public void Unreserve_MultipleTickets_ReleasesResourceAfterLastTicket()
    {
        var manager = CreateManager();
        var resource = RoutingResourceGenerator.GenerateResource();
        var firstAgvId = Guid.NewGuid();
        var secondAgvId = Guid.NewGuid();
        manager.TryReserve(resource, firstAgvId, out var firstTicket).ShouldBeTrue();
        manager.TryReserve(resource, firstAgvId, out var secondTicket).ShouldBeTrue();

        manager.Unreserve(firstTicket);

        manager.TryReserve(resource, secondAgvId, out _).ShouldBeFalse();

        manager.Unreserve(secondTicket);

        manager.TryReserve(resource, secondAgvId, out _).ShouldBeTrue();
    }

    [TestMethod]
    public void Unreserve_AlreadyUnreservedTicket_LogsWarning()
    {
        var logger = new Mock<ILogger<ReservationManager>>();
        var manager = new ReservationManager(logger.Object);
        var resource = RoutingResourceGenerator.GenerateResource();
        manager.TryReserve(resource, Guid.NewGuid(), out var ticket).ShouldBeTrue();
        manager.Unreserve(ticket);

        manager.Unreserve(ticket);

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static ReservationManager CreateManager()
    {
        return new ReservationManager(Mock.Of<ILogger<ReservationManager>>());
    }
}