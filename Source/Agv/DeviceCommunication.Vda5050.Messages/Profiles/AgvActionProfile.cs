using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
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
        CreateMap<MqttAgvActionParameter, AgvActionParameter>()
            .ForMember(a => a.IsOptional, opt => opt.MapFrom(ma => ma.IsOptional ?? false));
        CreateMap<MqttValueDataType, ValueDataType>();
        
        CreateMap<MqttAgvAction, AgvAction>()
            .ForMember(a => a.AgvSerialNumber, opt => opt.Ignore());
    }
}