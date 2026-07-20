using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;

/// <summary>
/// Provides a profile for state property mapping.
/// </summary>
public class MqttStateProfile: Profile
{
    /// <inheritdoc/>
    public MqttStateProfile()
    {
        CreateMap<MqttOperatingMode, AgvOperatingMode>().ReverseMap();
    }
}