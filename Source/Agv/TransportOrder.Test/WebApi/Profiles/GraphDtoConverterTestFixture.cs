using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

public class GraphDtoConverterTestFixture
{
    private readonly IMapper _mapper = MappingHelper.GetMapper(new NewGraphProfile());
    private readonly Graph _graph = NewGraphGenerator.GenerateMultiStateGraph();

    private readonly IReadOnlyList<LoadState> _loadStates =
    [
        new(NewGraphGenerator.AgvTypeAUnloadedId, isLoaded: false),
        new(NewGraphGenerator.AgvTypeALoadedId, isLoaded: true),
        new(NewGraphGenerator.AgvTypeBUnloadedId, isLoaded: false)
    ];

    public static NodeDto GetNode(GraphDto graph, string name)
    {
        return graph.Nodes.Single(n => n.Name == name);
    }

    public static EdgeDto GetEdge(GraphDto graph, string name)
    {
        return graph.Edges.Single(e => e.Name == name);
    }

    public GraphDto Map()
    {
        return _mapper.Map<GraphDto>((_graph, _loadStates));
    }
}