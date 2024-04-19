using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using MassTransit;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Controllers.ActionGroup;

public class ActionGroupsControllerTestFixture : MockRequestClientControllerTestFixture<ActionGroupsController>
{
    private readonly Mock<IRequestClient<GetActionGroups>> _getAgvActionGroupsRequest = new();
    private readonly Mock<IRequestClient<GetActionGroupCountsByState>> _getAgvActionGroupCountsByStateRequest = new();
    private readonly Mock<IRequestClient<GetActionGroupStatistics>> _durationStatisticsRequest = new();

    protected override IEnumerable<Profile> GetProfiles() => [new ActionProfile()];

    public ActionGroupsControllerTestFixture()
    {
        AddRequestClientMock(_getAgvActionGroupsRequest);
        AddRequestClientMock(_getAgvActionGroupCountsByStateRequest);
        AddRequestClientMock(_durationStatisticsRequest);
        Build();
    }

    public ActionGroupsControllerTestFixture WithDurationStatistics(ICollection<DurationStatistics> durationStatistics)
    {
        _durationStatisticsRequest.Setup(x => x.GetResponse<GotActionGroupStatistics>(
                    It.IsAny<GetActionGroupStatistics>(),
                    default,
                    default)
                .Result.Message)
            .Returns(new GotActionGroupStatistics(durationStatistics));
        return this;
    }

    public ActionGroupsControllerTestFixture WithActionGroups(IReadOnlyCollection<ActionGroupReadModel> actionGroups)
    {
        _getAgvActionGroupsRequest.Setup(x => x.GetResponse<GotActionGroups>(
                It.IsAny<GetActionGroups>(), default, default).Result.Message)
            .Returns(new GotActionGroups(actionGroups));
        return this;
    }

    public ActionGroupsControllerTestFixture WithActionGroupCount(IList<AgvGroupCountByState> groupCount)
    {
        _getAgvActionGroupCountsByStateRequest.Setup(x => x.GetResponse<GotActionGroupCountsByState>(
                    It.IsAny<GetActionGroupCountsByState>(),
                    default,
                    default)
                .Result.Message)
            .Returns(new GotActionGroupCountsByState(groupCount));
        return this;
    }
}