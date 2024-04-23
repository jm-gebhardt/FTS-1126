using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.ActionGroup;

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