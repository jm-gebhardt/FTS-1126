using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Profiles;

/// <summary>
/// Profile for mapping the <see cref="AgvWifiMetrics"/> to the <see cref="WifiMetrics"/> model.
/// </summary>
[UsedImplicitly]
public class AgvWifiMetricsProfile : Profile
{
    /// <inheritdoc />
    public AgvWifiMetricsProfile()
    {
        CreateMap<WifiMetrics, AgvWifiMetrics>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AgvCorrelationId, opt => opt.Ignore())
            .ForMember(dest => dest.GridCellId, opt => opt.Ignore())
            .ForMember(dest => dest.GridCell, opt => opt.Ignore());
    }
}