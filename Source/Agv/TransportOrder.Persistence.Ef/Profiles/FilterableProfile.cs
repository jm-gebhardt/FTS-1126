using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
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
        CreateMap(typeof(PagedResult<>), typeof(PagedResult<>));

        CreateMap<ActionGroupFilterable, FilteredActionGroup>();

        CreateMap<AgvActionFilterable, FilteredAgvAction>();

        CreateMap<AgvFilterable, FilteredAgv>();

        CreateMap<ActionTargetFilterable, FilteredActionTarget>();

        CreateMap<GraphNodeFilterable, FilteredGraphNode>();

        CreateMap<LogEntryFilterable, LogEntry>();

        CreateMap<AgvPositionFilterable, FilteredAgvPosition>();
    }
}