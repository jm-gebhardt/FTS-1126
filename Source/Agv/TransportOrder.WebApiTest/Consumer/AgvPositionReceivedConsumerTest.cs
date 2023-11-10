using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

/// <summary>
/// Test for AgvPositionReceivedConsumer
/// </summary>
[TestClass]
public class AgvPositionReceivedConsumerTest
{
    private const string AgvTestIdentifier01 = "#2410#";
    private const string AgvTestIdentifier02 = "#0815#";

    /// <summary>
    /// Test creation of repository and try to access AGV that doesn't exits.
    /// </summary>
    [TestMethod]
    public async Task PublishPosition_RepositoryCreated_AccessWrongAgv_NotFound()
    {
        var fixture = new AgvPositionReceivedConsumerMassTransitTestFixture();

        await fixture.TestHarness!.Bus.Publish(
            new AgvPositionReceived(
                AgvTestIdentifier01.ToGuid(),
                AgvTestIdentifier01,
                new AgvPosition(false, 0, 0, 0, 0)
            ));

        (await fixture.TestHarness.Published.Any<AgvPositionReceived>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        var repository = fixture.Provider!.GetRequiredService<IAgvPositionRepository>();
        repository.ShouldNotBeNull();

        var position = repository.GetPosition(Guid.Empty);
        position.ShouldBeNull();
    }

    /// <summary>
    /// Test access AGV position received by the repository
    /// </summary>
    [TestMethod]
    public async Task PublishTwoPosition_RepositoryCreated_AccessOneAgv_Found()
    {
        const bool init1 = true;
        const float x1 = 1f;
        const float y1 = 2f;
        const float z1 = 3f;
        const float theta1 = 3f;
        const float deviation1 = 0.123f;
        const bool init2 = false;
        const float x2 = 0f;
        const float y2 = 0f;
        const float z2 = 0f;
        const float theta2 = 0f;

        var fixture = new AgvPositionReceivedConsumerMassTransitTestFixture();

        await fixture.TestHarness!.Bus.Publish(
            new AgvPositionReceived(
                AgvTestIdentifier01.ToGuid(),
                AgvTestIdentifier01,
                new AgvPosition(init1, x1, y1, z1, theta1, deviation1)
            ));
        await fixture.TestHarness.Bus.Publish(
            new AgvPositionReceived(
                AgvTestIdentifier02.ToGuid(),
                AgvTestIdentifier02,
                new AgvPosition(init2, x2, y2, z2, theta2)
            ));

        (await fixture.TestHarness.Published.Any<AgvPositionReceived>()).ShouldBeTrue();
        fixture.ConsumerHarness!.Consumed.Select<AgvPositionReceived>().Count().ShouldBe(2);

        var repository = fixture.Provider!.GetRequiredService<IAgvPositionRepository>();
        repository.ShouldNotBeNull();

        var position = repository.GetPosition(AgvTestIdentifier01.ToGuid());
        position.ShouldNotBeNull();
        position.AgvPosition.PositionInitialized.ShouldBe(init1);
        position.AgvPosition.X.ShouldBe(x1, 0.00001);
        position.AgvPosition.Y.ShouldBe(y1, 0.00001);
        position.AgvPosition.Z.ShouldBe(z1, 0.00001);
        position.AgvPosition.Theta.ShouldBe(theta1, 0.00001);
        position.AgvPosition.DeviationRange!.Value.ShouldBe(deviation1, 0.00001);

        position = repository.GetPosition(AgvTestIdentifier02.ToGuid());
        position.ShouldNotBeNull();
        position.AgvPosition.X.ShouldBe(x2, 0.00001);
        position.AgvPosition.Y.ShouldBe(y2, 0.00001);
        position.AgvPosition.Z.ShouldBe(z2, 0.00001);
        position.AgvPosition.Theta.ShouldBe(theta2, 0.00001);
        position.AgvPosition.DeviationRange.ShouldBeNull();
    }

    /// <summary>
    /// Test for handling a AgvPositionStatus event
    /// </summary>
    [TestMethod]
    public async Task AgvPositionReceived_Consumed()
    {
        var fixture = new AgvPositionReceivedConsumerMassTransitTestFixture()
            .UseAgvPositionRepositoryMock()
            .Build();

        Mock<IAgvPositionRepository> agvPositionRepositoryMock = fixture.AgvPositionRepositoryMock!;

        await fixture.TestHarness!.Bus.Publish(
            new AgvPositionReceived(
                AgvTestIdentifier01.ToGuid(),
                AgvTestIdentifier01,
                new AgvPosition(false, 0, 0, 0, 0)
            ));

        (await fixture.TestHarness.Published.Any<AgvPositionReceived>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvPositionReceived>()).ShouldBeTrue();

        agvPositionRepositoryMock.Verify(h => h.UpdatePositionAsync(
            It.Is<AgvPositionStatus>(o => o.AgvId == AgvTestIdentifier01 && o.CorrelationId == AgvTestIdentifier01.ToGuid())), Times.Once);
    }
}