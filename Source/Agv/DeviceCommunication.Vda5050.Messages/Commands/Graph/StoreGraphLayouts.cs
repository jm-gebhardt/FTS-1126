using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;

/// <summary>
/// Command to store graph layouts. 
/// </summary>
[PublicAPI]
public record StoreGraphLayouts(ICollection<GraphLayout> Layouts);