using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Provide for agv instant action related dtos.
/// </summary>
public class AgvInstantActionProfile : WebApiProfile
{
    /// <inheritdoc />
    public AgvInstantActionProfile()
    {
        CreateMap<AgvInstantActionParameterDefinition, AgvInstantActionParameterDefinitionDto>();
        CreateMap<InstantActionDefinition, AgvInstantActionDefinitionDto>()
            .ForSourceMember(s => s.AgvCorrelationIds, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.CorrelationId, opt => opt.DoNotValidate());
    }
}