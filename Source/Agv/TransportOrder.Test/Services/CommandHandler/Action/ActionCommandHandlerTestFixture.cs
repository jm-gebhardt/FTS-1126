using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

public class ActionCommandHandlerTestFixture(TimeSpan? inactivityTimeout = null) : ActionGroupTestFixtureBase<ActionCommandHandler>(inactivityTimeout);