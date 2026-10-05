using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.Common.Messages.HostOrder.Commands;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;
using JetBrains.Annotations;
using MassTransit;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

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
        busRegistrationConfigurator.AddRequestClient<RetractHostOrder>();
    }

    public ActionCommandHandlerTestFixture WithMultipleRequiredActionGroups(ICollection<ActionGroup> actionGroups)
    {
        var setupSequence = ActionGroupRepository.SetupSequence(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>()));

        foreach (var actionGroup in actionGroups)
        {
            setupSequence = setupSequence.ReturnsAsync(actionGroup);
        }

        return this;
    }
}