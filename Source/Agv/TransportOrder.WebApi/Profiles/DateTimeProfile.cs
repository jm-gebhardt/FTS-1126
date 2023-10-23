using AutoMapper;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for date times.
/// </summary>
public class DateTimeProfile : Profile
{
    /// <summary>
    /// Constructor
    /// </summary>
    public DateTimeProfile()
    {
        CreateMap<DateTime, DateTime>().ConvertUsing(new CustomDateTimeConverter());
        CreateMap<DateTime?, DateTime?>().ConvertUsing(new CustomDateTimeConverter());
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