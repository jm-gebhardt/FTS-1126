using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class GuidExtensions
{
    public static GraphNode ToGraphNode(this Guid id) => new(id, id.ToString());

    public static BaseNode ToBaseNode(this Guid id) => new(id, id.ToString());
}