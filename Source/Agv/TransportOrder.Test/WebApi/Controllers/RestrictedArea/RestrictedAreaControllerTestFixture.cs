using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.RestrictedArea;

public class RestrictedAreaControllerTestFixture: MockRequestClientControllerTestFixture<RestrictedAreaController>
{
    protected override IEnumerable<Profile> GetProfiles() => [new RestrictedAreaProfile(), new PolygonPointProfile()];

    private GotRestrictedArea _gotRestrictedArea = null!;
    private RestrictedAreaAdded _restrictedAreaAdded = null!;
    
    protected override void SetupController()
    {
        AddRequestClient<GetRestrictedArea, GotRestrictedArea>(() => _gotRestrictedArea);
        AddRequestClient<AddRestrictedArea, RestrictedAreaAdded>(() => _restrictedAreaAdded);
        AddRequestClient<TryDeleteRestrictedArea, Success>(() => new Success());
        AddRequestClient<TryUpdateRestrictedArea, Success>(() => new Success());
    }

    public new RestrictedAreaControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public RestrictedAreaControllerTestFixture WithRestrictedArea(TransportOrder.Domain.Models.RestrictedArea.RestrictedArea restrictedArea)
    {
        _gotRestrictedArea = new GotRestrictedArea(restrictedArea);
        _restrictedAreaAdded = new RestrictedAreaAdded(restrictedArea);
        return this;
    }
}