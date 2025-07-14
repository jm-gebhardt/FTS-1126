using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Trigger;

public class TriggerCommandHandlerTestFixture: MassTransitConsumerTestFixture<TriggerCommandHandler>
{
    private readonly Mock<ITriggerRepository> _triggerRepository = new();
    public TransportOrder.Domain.Models.Trigger.Trigger UpdatedTrigger = null!;

    protected override IEnumerable<Profile> GetProfiles()
    {
        yield return new PolygonPointProfile();
    }

    public TriggerCommandHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _triggerRepository.Object);
        });
        _triggerRepository.Setup(x => x.AddOrUpdate(It.IsAny<TransportOrder.Domain.Models.Trigger.Trigger>(), It.IsAny<bool>()))
            .Callback((TransportOrder.Domain.Models.Trigger.Trigger t, bool _) => UpdatedTrigger = t);
        _triggerRepository.Setup(x => x.Update(It.IsAny<TransportOrder.Domain.Models.Trigger.Trigger>(), It.IsAny<bool>()))
            .Callback((TransportOrder.Domain.Models.Trigger.Trigger t, bool _) => UpdatedTrigger = t);
        Build();
    }

    public TriggerCommandHandlerTestFixture WithTrigger(TransportOrder.Domain.Models.Trigger.Trigger trigger)
    {
        _triggerRepository.Setup(x => x.GetById(It.IsAny<Guid>())).ReturnsAsync(trigger);
        return this;
    }
}