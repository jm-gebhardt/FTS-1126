using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.Common.Messages.HostOrder.Commands;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using JetBrains.Annotations;
using MassTransit;
using Moq;
using Moq.Language;
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

    private static bool _respondsSuccess;

    [UsedImplicitly]
    private class RetractHostOrderConsumer : IConsumer<RetractHostOrder>
    {
        public async Task Consume(ConsumeContext<RetractHostOrder> context)
        {
            if (_respondsSuccess)
            {
                await context.RespondAsync(new Success());
                return;
            }
            await context.RespondAsync(new Failed());
        }
    }

    protected override void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        base.ConfigureMassTransitTestHarness(busRegistrationConfigurator);
        busRegistrationConfigurator.AddConsumer<MockRequestConsumer>();
        busRegistrationConfigurator.AddConsumer<RetractHostOrderConsumer>();
        busRegistrationConfigurator.AddRequestClient<RetractHostOrder>();
    }

    public ActionCommandHandlerTestFixture WithHostOrderRetracted(bool retracted)
    {
        _respondsSuccess = retracted;
        return this;
    }

    public ActionCommandHandlerTestFixture WithMultipleRequiredActionGroups(ICollection<ActionGroup> actionGroups)
    {
        ISetupSequentialResult<Task<ActionGroup>> setupSequence = ActionGroupRepository.SetupSequence(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>()));

        foreach (var actionGroup in actionGroups)
        {
            setupSequence = setupSequence.ReturnsAsync(actionGroup);
        }

        return this;
    }
}