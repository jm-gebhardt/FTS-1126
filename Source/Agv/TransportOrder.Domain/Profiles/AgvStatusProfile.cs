using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.Acc.TransportOrder.Domain.Profiles;

/// <summary>
/// Provides mapping configurations for the agv status.
/// </summary>
public class AgvStatusProfile : Profile
{
    /// <inheritdoc />
    public AgvStatusProfile()
    {
        CreateMap<AgvStatus, Common.Domain.Models.AgvStatus>().ReverseMap();
    }
}