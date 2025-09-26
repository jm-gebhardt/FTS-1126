using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Profiles;

/// <summary>
/// Automapper profile for filterables.
/// </summary>
public class FilterableProfile : Profile
{
    /// <summary>
    /// ctor
    /// </summary>
    public FilterableProfile()
    {
        CreateMap<ActionGroupFilterable, FilteredActionGroup>(MemberList.Destination);

        CreateMap<AgvActionFilterable, FilteredAgvAction>(MemberList.Destination);

        CreateMap<AgvFilterable, FilteredAgv>(MemberList.Destination);

        CreateMap<ActionTargetFilterable, FilteredActionTarget>(MemberList.Destination);

        CreateMap<GraphNodeFilterable, FilteredGraphNode>(MemberList.Destination);

        CreateMap<AgvPositionFilterable, FilteredAgvPosition>(MemberList.Destination);
    }
}