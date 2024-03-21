using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvCargoGenerator
{
    public static IEnumerable<Cargo> GenerateCargos(int count, Guid agvCorrelationId, string loadPosition = "Pos-")
    {
        for (int i = 0; i < count; i++)
        {
            string loadId = "Load-" + i;
            yield return new Cargo(loadId.ToGuid(), agvCorrelationId, loadId, "Box", new Point3D(1.0f, 1.0f, 1.0f, 1.0f), new Cuboid(1.0f, 1.0f, 1.0f), loadPosition + i, 10d);
        }
    }
}