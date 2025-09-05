using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Graphs.Requests;

/// <summary>
/// Provides a request to get fill quantities for all queues.
/// </summary>
[PublicAPI]
public record GetQueueFillQuantities;