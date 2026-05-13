using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Profiles;

/// <summary>
/// Profile for AgvError
/// </summary>
[PublicAPI]
public class MqttErrorProfile : Profile
{
    /// <see cref="MqttErrorProfile" />
    public MqttErrorProfile()
    {
        CreateMap<AgvError, MqttError>()
            .ForSourceMember(a => a.AgvCorrelationId, opt => opt.DoNotValidate())
            .ForSourceMember(a => a.CorrelationId, opt => opt.DoNotValidate())
            .ForSourceMember(a => a.ErrorCode, opt => opt.DoNotValidate())
            .ForSourceMember(a => a.ErrorResolved, opt => opt.DoNotValidate())
            .ForSourceMember(a => a.ErrorReceived, opt => opt.DoNotValidate())
            .ForSourceMember(a => a.ErrorCategory, opt => opt.DoNotValidate())
            .ForSourceMember(a => a.ErrorFlag, opt => opt.DoNotValidate())
            .ForSourceMember(a => a.Source, opt => opt.DoNotValidate())
            .ForMember(e => e.ErrorHint, opt => opt.Ignore());
        CreateMap<ErrorReference, MqttErrorReference>();
    }
}