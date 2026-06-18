using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// A collection of lif layouts.
/// </summary>
[PublicAPI]
public class LifLayoutCollection
{
    /// <inheritdoc cref="LifLayoutCollection"/>
    public LifLayoutCollection(Layout[] layouts, MetaInformation metaInformation)
    {
        Layouts = layouts;
        MetaInformation = metaInformation;
    }

    /// <summary>
    /// Collection of layouts used in the facility by the driverless transport system. All layouts refer to the same global origin.
    /// </summary>
    public Layout[] Layouts { get; set; }

    /// <summary>
    /// Contains metadata about the project and the LIF file.
    /// </summary>
    public MetaInformation MetaInformation { get; set; }

    /// <summary>
    /// Get vehicle types supported by the layouts.
    /// </summary>
    /// <returns>VehicleTypeIds included in the layouts.</returns>
    public ICollection<string> GetSupportedVehicleTypes()
    {
        return Layouts.SelectMany(l => l.GetSupportedVehicleTypes()).Distinct().ToList();
    }
}
