using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;

/// <summary>
/// Event emitted after graph layouts have been stored successfully.
/// </summary>
/// <param name="LayoutIds">The layout Ids which have been stored.</param>
/// <param name="VehicleTypeIds">The included vehicle type Ids.</param>
[PublicAPI]
public record LayoutStored(ICollection<string> LayoutIds, ICollection<string> VehicleTypeIds);