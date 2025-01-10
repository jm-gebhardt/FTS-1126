using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;

/// <summary>
/// Connection states for a last will (see 'connection.schema')
/// </summary>
[PublicAPI]
public enum ConnectionState
{
    /// <summary/>
    Online,
    
    /// <summary/>
    Offline,
    
    /// <summary/>
    ConnectionBroken
}