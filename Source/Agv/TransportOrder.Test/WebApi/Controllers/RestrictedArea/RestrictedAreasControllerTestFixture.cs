using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.RestrictedArea;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.RestrictedArea;

public class RestrictedAreasControllerTestFixture : MockRequestClientControllerTestFixture<RestrictedAreasController>
{
    protected override IEnumerable<Profile> GetProfiles() => [new RestrictedAreaProfile(), new PolygonPointProfile()];

    private GotRestrictedAreas _gotRestrictedAreas = null!;

    protected override void SetupController()
    {
        AddRequestClient<GetRestrictedAreas, GotRestrictedAreas>(() => _gotRestrictedAreas);
    }

    public new RestrictedAreasControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public RestrictedAreasControllerTestFixture WithRestrictedArea(TransportOrder.Domain.Models.RestrictedArea.RestrictedArea restrictedArea)
    {
        _gotRestrictedAreas = new GotRestrictedAreas([restrictedArea]);
        return this;
    }
}