using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.CommandHandler.Action;

public class ActionCommandHandlerTestFixture(TimeSpan? inactivityTimeout = null) : ActionGroupTestFixtureBase<ActionCommandHandler>(inactivityTimeout);