using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.ActionGroup;

/// <summary>
/// Provides a test fixture for <see cref="ActionGroupStatisticsRequestHandler"/> .
/// </summary>
public class ActionGroupStatisticsRequestHandlerTestFixture : MediatorTestFixture<GetActionGroupStatistics, ActionGroupStatisticsRequestHandler>
{
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    public new ActionGroupStatisticsRequestHandlerTestFixture Build()
    {
        AddServices(s => s.AddTransient(_ => _actionGroupRepository.Object));
        base.Build();
        return this;
    }

    public ActionGroupStatisticsRequestHandlerTestFixture WithActionDurations(Dictionary<ActionType, ICollection<double>> actionDurations)
    {
        _actionGroupRepository.Setup(x => x.GetActionDurationsByTypeAsync(It.IsAny<ActionGroupState>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(actionDurations);
        return this;
    }
}