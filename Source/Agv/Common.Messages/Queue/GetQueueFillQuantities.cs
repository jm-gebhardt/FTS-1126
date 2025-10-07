using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// Provides a request to get fill quantities for all queues.
/// </summary>
[PublicAPI]
public record GetQueueFillQuantities;