using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.Services.Simulation.Data;

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