using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// Represents the response to <see cref="GetQueueFillQuantities"/>.
/// </summary>
/// <param name="FillQuantities">The fill quantities of all queues.</param>
[PublicAPI]
public record GotQueueFillQuantities(ICollection<QueueFillQuantity> FillQuantities);

/// <summary>
/// The queue fill quantity.
/// </summary>
/// <param name="Name">The queue name.</param>
/// <param name="Quantity">The number of agvs currently assigned to the queue.</param>
/// <param name="Capacity">The maximum capacity of the queue.</param>
/// <param name="EnteredAgvs">The number of agvs in the queue.</param>
[PublicAPI]
public record QueueFillQuantity(string Name, int Quantity, int Capacity, int EnteredAgvs);