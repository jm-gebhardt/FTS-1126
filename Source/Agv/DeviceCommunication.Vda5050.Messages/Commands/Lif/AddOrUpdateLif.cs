using JetBrains.Annotations;
using Vdma.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;

/// <summary>
/// Command to process a LIF layout collection.
/// </summary>
[PublicAPI]
public record AddOrUpdateLif(LifLayoutCollection LifLayoutCollection);