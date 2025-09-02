using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;

/// <summary>
/// Type of maintenance
/// </summary>
[PublicAPI]
public enum MaintenanceType
{
    /// <summary/>
    BatteryReplaced,

    /// <summary/>
    TiresReplaced,

    /// <summary/>
    DrivesReplaced,

    /// <summary/>
    SensorReplaced,

    /// <summary/>
    SensorsCleaned,

    /// <summary/>
    OtherPartReplaced,

    /// <summary/>
    Other
}