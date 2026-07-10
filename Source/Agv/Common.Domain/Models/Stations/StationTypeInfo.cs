using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.Stations;

/// <summary>
/// Describes a station and its node types.
/// </summary>
/// <param name="Name">The name of the station.</param>
/// <param name="PrimaryType">The primary type of the station.</param>
/// <param name="AdditionalTypes">Additional station types.</param>
[PublicAPI]
public record StationTypeInfo(string Name, AgvGraph.NodeType PrimaryType, ICollection<AgvGraph.NodeType> AdditionalTypes);