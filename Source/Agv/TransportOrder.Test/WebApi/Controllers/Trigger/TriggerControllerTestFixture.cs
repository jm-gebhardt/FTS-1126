using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Trigger;

public class TriggerControllerTestFixture: MockRequestClientControllerTestFixture<TriggerController>
{
    protected override IEnumerable<Profile> GetProfiles() => [new TriggerProfile()];

    private GotTrigger _gotTrigger = null!;
    private TriggerLocationAssigned _triggerLocationAssigned = null!;
    
    protected override void SetupController()
    {
        AddRequestClient<GetTrigger, GotTrigger>(() => _gotTrigger);
        AddRequestClient<AssignTriggerToLocation, TriggerLocationAssigned>(() => _triggerLocationAssigned);
        AddRequestClient<TryRemoveTriggerLocation, Success>(() => new Success());
        AddRequestClient<TryUpdateConfigurationParameter, Success>(() => new Success());
    }

    public new TriggerControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public TriggerControllerTestFixture WithTrigger(TransportOrder.Domain.Models.Trigger.Trigger trigger)
    {
        _gotTrigger = new GotTrigger(trigger);
        return this;
    }

    public TriggerControllerTestFixture WithTriggerLocation(Location location)
    {
        _triggerLocationAssigned = new TriggerLocationAssigned(location);
        return this;
    }
}