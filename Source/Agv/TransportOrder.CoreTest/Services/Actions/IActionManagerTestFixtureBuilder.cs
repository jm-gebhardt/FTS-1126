//ReSharper disable all
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

public interface IActionManagerTestFixtureBuilder : IMassTransitTestFixtureBuilder<ActionManagerTestFixture, IActionManagerTestFixtureBuilder>
{
    [UsedImplicitly]
    IActionManagerTestFixtureBuilder UseAssignAgvConsumer();

    /*
    [UsedImplicitly]
    IActionManagerTestFixtureBuilder SetActionCreationSucceededEventResponse(ActionCreationSucceededEvent actionCreationSucceededEvent);
    */

    [UsedImplicitly]
    IActionManagerTestFixtureBuilder SetAgvAssignmentResponse(AgvAssignmentSucceeded agvAssignmentSucceeded);

    [UsedImplicitly]
    IActionManagerTestFixtureBuilder SetAgvAssignmentResponse(AgvAssignmentFailed agvAssignmentFailed);
}