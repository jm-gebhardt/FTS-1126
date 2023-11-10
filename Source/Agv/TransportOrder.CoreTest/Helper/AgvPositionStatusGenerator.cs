using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvPositionStatusGenerator
{
    public static AgvPositionStatus Generate(float x = 1, float y = 2, float z = 3, Guid correlationId = default, float theta = 0, bool positionInitialized = true, float? deviationRange = null)
    {
        return new AgvPositionStatus(correlationId != default ? correlationId : Guid.NewGuid(), "agv", AgvPositionGenerator.Generate(x, y, z, theta, positionInitialized, deviationRange));
    }
}