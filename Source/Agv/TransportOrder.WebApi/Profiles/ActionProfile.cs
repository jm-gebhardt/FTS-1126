using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for Actions
/// </summary>
public class ActionProfile : WebApiProfile
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ActionProfile()
    {
        CreateMap<ActionGroupReadModel, ActionGroupDto>()
            .ForMember(g => g.EstimatedCompletionDate, opt => opt.MapFrom(g => g.StartedDate + g.EstimatedDuration))
            .ForSourceMember(g => g.HasPriority, opt => opt.DoNotValidate());
        CreateMap<AgvActionReadModel, AgvActionDto>();
        CreateMap<AgvGroupCountByState, AgvGroupCountByStateDto>();
        CreateMap<ActionGroup, ActionGroupDto>()
            .ForMember(g => g.EstimatedCompletionDate, opt => opt.MapFrom(g => g.StartedDate + g.EstimatedDuration));
        CreateMap<AgvAction, AgvActionDto>()
            .ForMember(m => m.TargetNodeType, opt => opt.Ignore())
            .ForMember(m => m.TargetNodeId, opt => opt.MapFrom(s => s.Target.NodeId!));
        CreateMap<DateTime, DateTime>().ConvertUsing(new CustomDateTimeConverter());
        CreateMap<DateTime?, DateTime?>().ConvertUsing(new CustomDateTimeConverter());
        CreateMap<DurationStatistics, ActionDurationStatisticsDto>();
    }

    /// <summary>
    /// Provides a custom converter for date time objects to
    /// add local time information.
    /// </summary>
    private class CustomDateTimeConverter : ITypeConverter<DateTime, DateTime>, ITypeConverter<DateTime?, DateTime?>
    {
        /// <inheritdoc/>
        public DateTime Convert(DateTime source, DateTime target, ResolutionContext context)
        {
            switch (source.Kind)
            {
                case DateTimeKind.Unspecified:
                    return DateTime.SpecifyKind(source, DateTimeKind.Utc);

                case DateTimeKind.Local:
                    return TimeZoneInfo.ConvertTime(source, TimeZoneInfo.Utc);

                default:
                    return source;
            }
        }

        /// <inheritdoc/>
        public DateTime? Convert(DateTime? source, DateTime? target, ResolutionContext context)
        {
            if (source == null)
            {
                return null;
            }
            var sourceTime = (DateTime)source;
            switch (sourceTime.Kind)
            {
                case DateTimeKind.Unspecified:
                    return DateTime.SpecifyKind(sourceTime, DateTimeKind.Utc);

                case DateTimeKind.Local:
                    return TimeZoneInfo.ConvertTime(sourceTime, TimeZoneInfo.Utc);

                default:
                    return sourceTime;
            }
        }
    }
}