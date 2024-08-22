using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Trigger;

public class TriggerCommandHandlerTestFixture: MassTransitConsumerTestFixture<TriggerCommandHandler>
{
    private readonly Mock<ITriggerRepository> _triggerRepository = new();
    public TransportOrder.Domain.Models.Trigger.Trigger Trigger = null!;

    public TriggerCommandHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _triggerRepository.Object);
            s.AddTransient<IMapper>(_ => MappingHelper.GetMapper(new PolygonPointProfile()));
        });
        _triggerRepository.Setup(x => x.Add(It.IsAny<TransportOrder.Domain.Models.Trigger.Trigger>()))
            .Callback((TransportOrder.Domain.Models.Trigger.Trigger t) => Trigger = t);
        _triggerRepository.Setup(x => x.Update(It.IsAny<TransportOrder.Domain.Models.Trigger.Trigger>()))
            .Callback((TransportOrder.Domain.Models.Trigger.Trigger t) => Trigger = t);
        Build();
    }

    public TriggerCommandHandlerTestFixture WithTrigger(TransportOrder.Domain.Models.Trigger.Trigger trigger)
    {
        _triggerRepository.Setup(x => x.GetById(It.IsAny<Guid>())).ReturnsAsync(trigger);
        return this;
    }
}