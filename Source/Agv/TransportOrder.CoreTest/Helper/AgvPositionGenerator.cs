using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvPositionGenerator
{
    public static AgvPosition Generate(float x = 1, float y = 2, float z = 3, float theta = 0, bool positionInitialized = true, float? deviationRange = null)
    {
        return new AgvPosition(positionInitialized, x, y, z, theta, deviationRange);
    }
}