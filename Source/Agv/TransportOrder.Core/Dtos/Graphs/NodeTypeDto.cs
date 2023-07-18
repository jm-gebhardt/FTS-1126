namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.Graphs;

/// <summary>
/// Node types.
/// <para>
/// These types are coming from the specification.
/// </para>
/// </summary>
public enum NodeTypeDto
{
    /// <summary>General WayPoint</summary>
    [JetBrains.Annotations.UsedImplicitly]
    WayPoint,

    /// <summary>Charging station</summary>
    [JetBrains.Annotations.UsedImplicitly]
    Charge,

    /// <summary>Pick station</summary>
    [JetBrains.Annotations.UsedImplicitly]
    Pick,

    /// <summary>Drop station</summary>
    [JetBrains.Annotations.UsedImplicitly]
    Drop,

    /// <summary>Pick/Drop station</summary>
    [JetBrains.Annotations.UsedImplicitly]
    PickDrop,

    /// <summary>Waiting point</summary>
    [JetBrains.Annotations.UsedImplicitly]
    Park
}