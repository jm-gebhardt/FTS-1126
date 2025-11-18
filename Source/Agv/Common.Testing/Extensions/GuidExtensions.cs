using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class GuidExtensions
{
    extension(Guid id)
    {
        public GraphNode ToGraphNode() => new(id, id.ToString());

        public BaseNode ToBaseNode() => new(id, id.ToString());
    }
}