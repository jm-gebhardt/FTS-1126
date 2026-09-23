using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Profiles;

/// <summary>
/// Provides mapping for agv type simulation configurations.
/// </summary>
[UsedImplicitly]
public class AgvTypeConfigurationProfile : Profile
{
    /// <inheritdoc/>
    public AgvTypeConfigurationProfile()
    {
        CreateMap<UpdateAgvTypeConfigurationDto, AgvTypeSimulationSettings>();
        CreateMap<AddAgvTypeConfigurationDto, AgvTypeSimulationSettings>();
        CreateMap<SimulatedAgvTypeConfiguration, AgvTypeConfigurationDto>()
            .ForMember(dto => dto.AgvTypeId, options => options.MapFrom(configuration => configuration.Id))
            .ForMember(dto => dto.SerialNumbers, options => options.MapFrom(configuration => configuration.SerialNumbers()));
    }
}