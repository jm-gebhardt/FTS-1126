using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;

/// <summary>
/// Provides mapping configurations for the agv status.
/// </summary>
public class AgvStatusProfile : Profile
{
    /// <inheritdoc />
    public AgvStatusProfile()
    {
        CreateMap<AgvStatus, Common.Messages.AgvManagement.AgvStatus>().ReverseMap();
    }
}