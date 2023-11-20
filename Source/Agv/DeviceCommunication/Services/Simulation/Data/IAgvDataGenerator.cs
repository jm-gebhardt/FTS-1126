using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Data;

/// <summary>
/// Agv Data Generator
/// </summary>
public interface IAgvDataGenerator
{
    /// <summary>
    /// Generate Agv Data
    /// </summary>
    /// <param name="agvIdx">Index of the generated AGV</param>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    public AgvData Generate(int agvIdx, Guid correlationId);
}