using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Profiles;

/// <summary>
/// Provides mapping for agv positions.
/// </summary>
[UsedImplicitly]
public class AgvPositionProfile : Profile
{
    /// <inheritdoc/>
    public AgvPositionProfile()
    {
        CreateMap<AgvPositionDto, AgvPosition>();
    }
}