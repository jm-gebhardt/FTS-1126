using Gebhardt.Acc.Common.Messages.Route;
using Gebhardt.Acc.Common.Messages.Route.AgvActions;
using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.Common.Testing.Fixtures;

namespace Gebhardt.Acc.CommonTest.Messages;

[TestClass]
public class RouteMessagesTest
{
    [TestMethod]
    public void RoutePlanned_ValidData_MessageSerialized()
    {
        var fixture = new MessageSerialisationTestFixture();
        var routePlanned = new RoutePlanned(Guid.NewGuid(), Guid.NewGuid(), new[]
        {
            new RouteStep(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null),
            new RouteStep(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), new PickAction(1.0)),
            new RouteStep(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), new DropAction(null)),
            new RouteStep(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), new ChargeAction())
        });

        fixture.ValidateSerialization(routePlanned);
    }

    [TestMethod]
    public void RouteStepsProcessed_ValidData_MessageSerialized()
    {
        var fixture = new MessageSerialisationTestFixture();
        var routeStepsProcessed = new RouteStepsProcessed(Guid.NewGuid(), Guid.NewGuid(), new[]
        {
            new RouteStepState(Guid.NewGuid(), RouteStepStatus.Acknowledged),
            new RouteStepState(Guid.NewGuid(), RouteStepStatus.Completed),
            new RouteStepState(Guid.NewGuid(), RouteStepStatus.Failed, "failure reason")
        });

        fixture.ValidateSerialization(routeStepsProcessed);
    }
}