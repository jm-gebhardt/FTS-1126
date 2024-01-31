using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Data;

/// <inheritdoc cref="AgvData" />
public record KarisType1 : AgvData
{
    /// <summary>
    /// Karis Type 1
    /// </summary>
    /// <param name="correlationId">AGV CorrelationId</param>
    /// <param name="ipV4Address">IP Address</param>
    public KarisType1(Guid correlationId, string ipV4Address) : base(
        correlationId,
        "Karis",
        "Type 1",
        ipV4Address,
        "v42.3",
        100,
        1,
        4,
        3,
        1.5,
        Mathematics.ConvertToRadians(5),
        0.562,
        0.687,
        40,
        new Sensor[] { new() { SensorIdentifier = "Sensor 1" } }
    )
    {
    }
}

/// <inheritdoc cref="AgvData" />
public record KarisType2 : AgvData
{
    /// <summary>
    /// Karis Type 2
    /// </summary>
    /// <param name="correlationId">AGV CorrelationId</param>
    /// <param name="ipV4Address">IP Address</param>
    public KarisType2(Guid correlationId, string ipV4Address) : base(
        correlationId,
        "Karis",
        "Type 2",
        ipV4Address,
        "v42.3",
        100,
        1,
        3,
        2,
        1,
        Mathematics.ConvertToRadians(5),
        0.687,
        1.087,
        50,
        new Sensor[]
        {
            new() { SensorIdentifier = "Sensor 1" },
            new() { SensorIdentifier = "Sensor 2" }
        }
    )
    {
    }
}

/// <inheritdoc cref="AgvData" />
public record KarisType3 : AgvData
{
    /// <summary>
    /// Karis Type 3
    /// </summary>
    /// <param name="correlationId">AGV CorrelationId</param>
    /// <param name="ipV4Address">IP Address</param>
    public KarisType3(Guid correlationId, string ipV4Address) : base(
        correlationId,
        "Karis",
        "Type 3",
        ipV4Address,
        "v42.3",
        100,
        1,
        2,
        1,
        0.5,
        Mathematics.ConvertToRadians(5),
        0.892,
        1.292,
        60,
        new Sensor[]
        {
            new() { SensorIdentifier = "Sensor 1" },
            new() { SensorIdentifier = "Sensor 2" },
            new() { SensorIdentifier = "Sensor 3" }
        }
    )
    {
    }
}