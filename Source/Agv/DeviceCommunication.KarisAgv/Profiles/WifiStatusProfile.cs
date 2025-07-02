using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Profiles;

/// <summary>
/// Profile for MqttWifiStatus
/// </summary>
[UsedImplicitly]
public class WifiStatusProfile : Profile
{
    /// <inheritdoc cref="WifiStatusProfile" />
    public WifiStatusProfile()
    {
        CreateMap<MqttWifiStatus, WifiMetrics>()
            .ForCtorParam(nameof(WifiMetrics.Timestamp),
                o => o.MapFrom((_, ctx) => (DateTime) ctx.Items[nameof(WifiMetrics.Timestamp)]))
            .ForCtorParam(nameof(WifiMetrics.Band), m => m.MapFrom(src => src.Band == string.Empty ? null : src.Band))
            // Channel 0 does technically not exist, so we map it to null
            .ForCtorParam(nameof(WifiMetrics.Channel), m => m.MapFrom(src => ConvertZeroToNull(src.Channel)))
            // Frequency 0 is not valid, so we map it to null
            .ForCtorParam(nameof(WifiMetrics.Frequency), m => m.MapFrom(src => ConvertZeroToNull(src.Frequency)))
            // Although physically valid, a value of 0 dBm is unrealistic for received beacons; usually means "not available"
            .ForCtorParam(nameof(WifiMetrics.BeaconSignalAverage), m => m.MapFrom(src => ConvertZeroToNull(src.BeaconSignalAverage)))
            // Noise of 0 dBm is interpreted as a placeholder
            .ForCtorParam(nameof(WifiMetrics.Noise), m => m.MapFrom(src => ConvertZeroToNull(src.Noise)));
    }

    private static int? ConvertZeroToNull(int v) => v == 0 ? null : v;
}