using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.ActionGroup;

public class ActionGroupsControllerTestFixture : MockRequestClientControllerTestFixture<ActionGroupsController>
{
    private GotActionGroupStatistics _gotActionGroupStatistics = null!;
    private GotActionGroups _gotActionGroups = null!;
    private GotActionGroupCountsByState _gotActionGroupCountsByState = null!;

    protected override IEnumerable<Profile> GetProfiles() => [new ActionProfile()];

    protected override void SetupController()
    {
        AddRequestClient<GetActionGroupStatistics, GotActionGroupStatistics>(() => _gotActionGroupStatistics);
        AddRequestClient<GetActionGroups, GotActionGroups>(() => _gotActionGroups);
        AddRequestClient<GetActionGroupCountsByState, GotActionGroupCountsByState>(() => _gotActionGroupCountsByState);
    }
    
    public new ActionGroupsControllerTestFixture Build()
    {
        base.Build();
        return this;
    }
    
    public ActionGroupsControllerTestFixture WithDurationStatistics(ICollection<DurationStatistics> durationStatistics)
    {
        _gotActionGroupStatistics = new GotActionGroupStatistics(durationStatistics);
        return this;
    }

    public ActionGroupsControllerTestFixture WithActionGroups(IReadOnlyCollection<ActionGroupReadModel> actionGroups)
    {
        _gotActionGroups = new GotActionGroups(actionGroups);
        return this;
    }

    public ActionGroupsControllerTestFixture WithActionGroupCount(IList<AgvGroupCountByState> groupCount)
    {
        _gotActionGroupCountsByState = new GotActionGroupCountsByState(groupCount);
        return this;
    }
}