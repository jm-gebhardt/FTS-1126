using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;

/// <summary>
/// Profile for AgvError
/// </summary>
public class MqttErrorProfile : Profile
{
    /// <see cref="MqttErrorProfile" />
    public MqttErrorProfile()
    {
        CreateMap<MqttError, AgvError>()
            .ForMember(nameof(AgvError.CorrelationId), opt => opt.Ignore())
            .ForMember(nameof(AgvError.AgvCorrelationId), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorReceived), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorTitle), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorCode), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorResolved), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorCategory), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorFlag), opt => opt.MapFrom(src => src.ErrorDescription!.ToUpper().Contains(nameof(ErrorFlag.Battery).ToUpper()) ? ErrorFlag.Battery : ErrorFlag.Agv))
            .ForMember(e => e.Source, opt => opt.MapFrom(src => AgvErrorSource.Agv));
        // Map only AgvCorrelationId to AgvError
        CreateMap<Guid, AgvError>(MemberList.None)
            .ForMember(nameof(AgvError.AgvCorrelationId), opt => opt.MapFrom(src => src));
        CreateMap<MqttErrorReference, ErrorReference>();
        CreateMap<MqttInfoReference, ErrorReference>();
        CreateMap<MqttInformation, AgvError>()
            .ForMember(nameof(AgvError.CorrelationId), opt => opt.Ignore())
            .ForMember(nameof(AgvError.AgvCorrelationId), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorReceived), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorTitle), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorCode), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorResolved), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorCategory), opt => opt.Ignore())
            .ForMember(nameof(AgvError.ErrorType), opt => opt.MapFrom(src => src.InfoType))
            .ForMember(nameof(AgvError.ErrorLevel), opt => opt.MapFrom(src => ErrorLevel.Information))
            .ForMember(nameof(AgvError.ErrorDescription), opt => opt.MapFrom(src => src.InfoDescription))
            .ForMember(nameof(AgvError.ErrorReferences), opt => opt.MapFrom(src => src.InfoReferences))
            .ForMember(nameof(AgvError.ErrorFlag), opt => opt.MapFrom(src => src.InfoDescription!.ToUpper().Contains(nameof(ErrorFlag.Battery).ToUpper()) ? ErrorFlag.Battery : ErrorFlag.Agv))
            .ForMember(e => e.Source, opt => opt.MapFrom(src => AgvErrorSource.Agv));
    }
}