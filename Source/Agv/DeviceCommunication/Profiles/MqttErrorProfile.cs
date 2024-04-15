using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.MqttMessages;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Profiles;

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
            .ForMember(nameof(AgvError.ErrorCategory), opt => opt.Ignore());
        // Map only AgvCorrelationId to AgvError
        CreateMap<Guid, AgvError>(MemberList.None)
            .ForMember(nameof(AgvError.AgvCorrelationId), opt => opt.MapFrom(src => src));
        CreateMap<MqttErrorReference, ErrorReference>();
    }
}