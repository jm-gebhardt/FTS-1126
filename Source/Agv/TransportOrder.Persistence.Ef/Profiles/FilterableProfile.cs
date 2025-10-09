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
        CreateMap<ActionGroupFilterReadModel, FilteredActionGroup>(MemberList.Destination);

        CreateMap<AgvActionFilterReadModel, FilteredAgvAction>(MemberList.Destination);

        CreateMap<AgvFilterReadModel, FilteredAgv>(MemberList.Destination);

        CreateMap<ActionTargetReadModel, FilteredActionTarget>(MemberList.Destination);

        CreateMap<GraphNodeReadModel, FilteredGraphNode>(MemberList.Destination);

        CreateMap<AgvPositionReadModel, FilteredAgvPosition>(MemberList.Destination);
    }
}