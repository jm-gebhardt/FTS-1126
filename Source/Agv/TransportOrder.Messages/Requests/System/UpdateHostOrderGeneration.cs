using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.System;

/// <summary>
/// Command to update the HostOrder generation state.
/// </summary>
/// <param name="IsEnabled">Indicates whether HostOrder generation is enabled.</param>
[UsedImplicitly]
public record UpdateHostOrderGeneration(bool IsEnabled);