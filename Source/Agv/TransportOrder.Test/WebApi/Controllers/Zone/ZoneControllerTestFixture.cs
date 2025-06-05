using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Zone.Queries;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Zone;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Zone;

public class ZoneControllerTestFixture : MockRequestClientControllerTestFixture<ZoneController>
{
    private GotZones _zones = new ([]);

    protected override IEnumerable<Profile> GetProfiles() => [new ZoneProfile()];

    protected override void SetupController()
    {
        AddRequestClient<GetZones, GotZones>(() => _zones);
    }

    public new ZoneControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public ZoneControllerTestFixture WithZones(ICollection<TransportOrder.Domain.Models.Zone.Zone> zones)
    {
        _zones = new GotZones(zones);
        return this;
    }
}