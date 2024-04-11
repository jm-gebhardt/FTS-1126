using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.EventHandler;

public class ActionEventHandlerTestFixture : MassTransitConsumerTestFixture<ActionEventHandler>
{
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<IAgvDb> _agvDb = new();

    protected override MassTransitConsumerTestFixture<ActionEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient(_ => _agvDb.Object);
        return this;
    }

    public ActionEventHandlerTestFixture()
    {
        Build();
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((ActionGroup?)null);
    }

    public ActionEventHandlerTestFixture AddAgvs(params TransportOrder.Domain.Models.Agv.Agv[] agvs)
    {
        _agvDb.AddAgvsToMockRepository(agvs);

        return this;
    }

    public static IMapper GetMapper()
    {
        return MappingHelper.GetMapper(new ActionProfile());
    }

    public ActionEventHandlerTestFixture AddAssignedAgv(Guid? correlationId)
    {
        _actionGroupRepository.Setup(x => x.GetAgvAssignedToAction(It.IsAny<Guid>())).ReturnsAsync(correlationId);
        return this;
    }

    public ActionEventHandlerTestFixture AddActionGroup(ActionGroup actionGroup)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRepository.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRepository.Setup(x => x.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        return this;
    }

    public ActionEventHandlerTestFixture WithCurrentAgvAction(AgvAction agvAction)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentAgvActionAsync(It.IsAny<Guid>())).ReturnsAsync(agvAction);
        return this;
    }

    public ActionEventHandlerTestFixture WithAgvSettings(AgvSettingsReadModel agvSettingsReadModel)
    {
        _agvDb.Setup(x => x.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(agvSettingsReadModel);
        return this;
    }
}