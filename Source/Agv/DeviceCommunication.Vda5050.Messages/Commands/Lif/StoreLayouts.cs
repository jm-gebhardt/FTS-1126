using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;

/// <summary>
/// Command to store lif layouts. 
/// </summary>
[PublicAPI]
public record StoreLayouts(ICollection<Layout> Layouts);