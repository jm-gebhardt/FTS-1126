using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Action;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;

/// <summary>
/// Provides mapping for <see cref="MqttAgvAction"/>
/// </summary>
public class AgvActionProfile: Profile
{
    /// <inheritdoc cref="AgvActionProfile" />
    public AgvActionProfile()
    {
        CreateMap<MqttActionScope, ActionScope>();
        CreateMap<MqttAgvActionParameter, AgvActionParameter>();
        CreateMap<MqttValueDataType, ValueDataType>();
        CreateMap<MqttBlockingType, BlockingType>();
        CreateMap<MqttAgvAction, AgvAction>()
            .ForMember(a => a.AgvSerialNumber, opt => opt.Ignore());
    }
}