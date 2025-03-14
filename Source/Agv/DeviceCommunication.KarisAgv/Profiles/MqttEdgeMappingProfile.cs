using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Profiles;

/// <summary>
/// Profile for <see cref="MqttEdgeMapping"/>
/// </summary>
public class MqttEdgeMappingProfile: Profile
{
    /// <inheritdoc cref="MqttEdgeMappingProfile" />
    public MqttEdgeMappingProfile()
    {
        CreateMap<MqttEdgeMapping?, MqttEdge?>()
            .ForSourceMember(s =>s!.InternalEdge, opt => opt.DoNotValidate());
    }
}