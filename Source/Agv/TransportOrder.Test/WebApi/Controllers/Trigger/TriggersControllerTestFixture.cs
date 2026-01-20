using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Trigger;

public class TriggersControllerTestFixture : MockRequestClientControllerTestFixture<TriggersController>
{
    protected override IEnumerable<Profile> GetProfiles() => [new TriggerProfile(), new PolygonPointProfile()];

    private GotTriggers _gotTriggers = null!;
    private GotDeletedTriggers _gotDeletedTriggers = null!;

    protected override void SetupController()
    {
        AddRequestClient<GetTriggers, GotTriggers>(() => _gotTriggers);
        AddRequestClient<GetDeletedTriggers, GotDeletedTriggers>(() => _gotDeletedTriggers);
    }

    public new TriggersControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public TriggersControllerTestFixture WithTriggers(ICollection<TransportOrder.Domain.Models.Trigger.Trigger> triggers)
    {
        _gotTriggers = new GotTriggers(triggers);
        return this;
    }

    public TriggersControllerTestFixture WithDeletedTriggers(ICollection<TransportOrder.Domain.Models.Trigger.Trigger> deletedTriggers)
    {
        _gotDeletedTriggers = new GotDeletedTriggers(deletedTriggers);
        return this;
    }
}

