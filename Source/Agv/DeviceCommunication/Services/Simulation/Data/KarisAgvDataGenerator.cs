using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Data;

/// <summary>
/// Karis Agv Data Generator where types are generated in a round robin fashion
/// </summary>
public class KarisAgvDataGenerator : IAgvDataGenerator
{
    private const string IpAddressBase = "192.168.0.2";

    /// <summary>
    /// Karis Types
    /// </summary>
    private enum AgvType
    {
        /// <summary>
        /// Type 1
        /// </summary>
        Type1,

        /// <summary>
        /// Type 2
        /// </summary>
        Type2,

        /// <summary>
        /// Type 3
        /// </summary>
        Type3
    }

    /// <inheritdoc />
    public AgvData Generate(int agvIdx, Guid correlationId)
    {
        var agvType = RoundRobinAgvType(agvIdx);
        return agvType switch
        {
            AgvType.Type1 => new KarisType1(correlationId, GetIpAddress(agvIdx)),
            AgvType.Type2 => new KarisType2(correlationId, GetIpAddress(agvIdx)),
            AgvType.Type3 => new KarisType3(correlationId, GetIpAddress(agvIdx)),
            _ => new KarisType1(correlationId, GetIpAddress(agvIdx))
        };
    }

    /// <summary>
    /// Round Robin Agv Type
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private static AgvType RoundRobinAgvType(int index)
    {
        int typeCount = Enum.GetNames(typeof(AgvType)).Length;
        return (AgvType)(index % typeCount);
    }

    /// <summary>
    /// Get successive IP Addresses
    /// Will repeat after 254 AGVs
    /// </summary>
    /// <returns></returns>
    private static string GetIpAddress(int agvIdx)
    {
        const string separator = ".";
        string[] address = IpAddressBase.Split(separator);
        address[^1] = $"{(int.Parse(address[^1]) + agvIdx) % 255}";
        return string.Join(separator, address);
    }
}