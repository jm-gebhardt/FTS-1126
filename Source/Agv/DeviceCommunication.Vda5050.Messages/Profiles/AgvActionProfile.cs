using AutoMapper;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
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

        CreateMap<AgvAction, AgvInstantAction>()
            .ForMember(ia => ia.Description, opt => opt.MapFrom(a => a.ActionDescription ?? string.Empty))
            .ForMember(ia => ia.ResultDescription, opt => opt.MapFrom(a => a.ResultDescription ?? string.Empty))
            .ForMember(ia => ia.Parameters, opt => opt.MapFrom(a => a.ActionParameters))
            .ForSourceMember(a => a.ActionScopes, opt => opt.DoNotValidate())
            .ForSourceMember(a => a.AgvSerialNumber, opt => opt.DoNotValidate());

        CreateMap<AgvActionParameter, AgvInstantActionParameterDefinition>()
            .ForMember(iap => iap.ValueType, opt => opt.MapFrom(a => a.ValueDataType))
            .ForMember(iap => iap.Description, opt => opt.MapFrom(a => a.Description ?? string.Empty));
    }
}