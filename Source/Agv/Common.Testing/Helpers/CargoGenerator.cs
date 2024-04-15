using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

public static class CargoGenerator
{
    public static ICollection<Cargo> GenerateCargos(Guid agvCorrelationId, string loadId = "LoadId", string loadType = "LoadType", string? loadPosition = null, double weight = 20)
    {
        var cargos = new List<Cargo>
        {
            new(loadId.ToGuid(), agvCorrelationId, loadId, loadType, new Point3D(1.0f, 1.0f, 1.0f, 1.0f), new Cuboid(1.0f, 1.0f, 1.0f), loadPosition, weight)
        };

        return cargos;
    }
}