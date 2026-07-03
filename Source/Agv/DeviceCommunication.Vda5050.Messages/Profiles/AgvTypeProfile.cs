using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;

/// <summary>
/// Provides mapping configurations for AGV types.
/// </summary>
[UsedImplicitly]
public class AgvTypeProfile : Profile
{
    /// <inheritdoc/>
    public AgvTypeProfile()
    {
        CreateMap<MqttFactsheet, AgvType>()
            .ConstructUsing(f => new AgvType(
                $"{f.Manufacturer}.{f.TypeSpecification.SeriesName}",
                f.Manufacturer,
                f.TypeSpecification.SeriesName,
                f.PhysicalParameters.Length,
                f.PhysicalParameters.Width,
                string.Empty,
                false))
            .ForAllMembers(opt => opt.Ignore());
    }
}