using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Profiles;

/// <summary>
/// Provides a profile for state property mapping.
/// </summary>
[UsedImplicitly]
public class MqttStateProfile: Profile
{
    /// <inheritdoc/>
    public MqttStateProfile()
    {
        CreateMap<MqttOperatingMode, AgvOperatingMode>().ReverseMap();
    }
}