using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Graphs.Requests;

/// <summary>
/// Represents the response to <see cref="GetQueueFillQuantities"/>.
/// </summary>
/// <param name="FillQuantities">The fill quantities of all queues.</param>
public record GotQueueFillQuantities(ICollection<QueueFillQuantity> FillQuantities);

/// <summary>
/// The queue fill quantity.
/// </summary>
/// <param name="Name">The queue name.</param>
/// <param name="Quantity">The number of agvs currently in the queue.</param>
/// <param name="Capacity">The maximum capacity of the queue.</param>
[PublicAPI]
public record QueueFillQuantity(string Name, int Quantity, int Capacity);