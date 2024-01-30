using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

public class ActionEventHandlerTestFixture : MassTransitConsumerTestFixtureWithScheduler<ActionEventHandler>
{
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient(_ => _actionGroupRepository.Object);
    }

    public ActionEventHandlerTestFixture()
    {
        Build();
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((ActionGroup?)null);
    }

    public static IMapper GetMapper()
    {
        return MappingHelper.GetMapper(new ActionProfile());
    }

    public void AddAssignedAgv(Guid? correlationId)
    {
        _actionGroupRepository.Setup(x => x.GetAgvAssignedToAction(It.IsAny<Guid>())).ReturnsAsync(correlationId);
    }

    public void AddActionGroup(ActionGroup actionGroup)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRepository.Setup(x => x.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
    }
}