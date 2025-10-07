using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Action;

public class ActionCommandHandlerTestFixture(TimeSpan? inactivityTimeout = null) : ActionGroupTestFixtureBase<ActionCommandHandler>(inactivityTimeout)
{
    [UsedImplicitly]
    private class MockRequestConsumer : IConsumer<TriggerDoppelspielPartnerQueueing>
    {
        public async Task Consume(ConsumeContext<TriggerDoppelspielPartnerQueueing> context)
        {
            await context.RespondAsync(new DoppelspielPartnerQueued(null));
        }
    }

    protected override void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        base.ConfigureMassTransitTestHarness(busRegistrationConfigurator);
        busRegistrationConfigurator.AddConsumer<MockRequestConsumer>();
    }
}