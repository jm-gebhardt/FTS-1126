using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

public static class AgvDataGenerator
{
    public static AgvData GenerateAgvData(Guid? correlationId = null, double width = 2.0, double length = 1.0, AgvData.Sensor[]? sensors = null)
    {
        return new AgvData(correlationId ?? Guid.NewGuid(),  
            "Karis", 
            "BF1", 
            "127.0.0.1", 
            1.8, 
            120.0, 
            0.6, 
            0.8, 
            0.7,
            Mathematics.ConvertToRadians(5),
            width, 
            length, 
            1000, 
            sensors ?? Array.Empty<AgvData.Sensor>());
    }
}