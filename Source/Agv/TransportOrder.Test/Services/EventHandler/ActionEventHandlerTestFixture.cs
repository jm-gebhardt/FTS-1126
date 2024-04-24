using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

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
        _agvDb.AddAgvs(agvs);

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