using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Data;

/// <summary>
/// Karis Agv Data Generator where types are generated in a round-robin fashion
/// </summary>
public class KarisAgvDataGenerator: IAgvDataGenerator
{
    private readonly int[] _karisTypes = [1, 2, 3];

    private const string IpAddressBase = "192.168.0.2";

    /// <summary>
    /// Get karis types from command line.
    /// </summary>
    public KarisAgvDataGenerator(int[]? karisTypes)
    {
        if (karisTypes == null || karisTypes.Length == 0)
        {
            return;
        }
        if (karisTypes.All(k => k is 1 or 2 or 3))
        {
            _karisTypes = karisTypes;
        }
        else
        {
            string karisTypeString = string.Join(",", karisTypes);
            throw new ArgumentException($"Option '--KarisTypes' contains invalid values [{karisTypeString}]. Valid values are 1, 2, or 3.");
        }
    }

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
    private AgvType RoundRobinAgvType(int index)
    {
        int typeCount = _karisTypes.Length;
        int arrayIndex = index % typeCount;

        return (AgvType)(_karisTypes[arrayIndex]  - 1);
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