using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.FactSheet;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Dtos.FactSheet;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Profiles;

/// <summary>
/// Provides mapping from <see cref="UploadFactsheetDto"/> to <see cref="Factsheet"/>.
/// </summary>
[UsedImplicitly]
public class FactsheetProfile : Profile
{
    /// <inheritdoc cref="FactsheetProfile" />
    public FactsheetProfile()
    {
        CreateMap<TypeSpecificationDto, TypeSpecification>();
        CreateMap<PhysicalParametersDto, PhysicalParameters>();
        CreateMap<MaxStringLensDto, MaxStringLens>();
        CreateMap<MaxArrayLensDto, MaxArrayLens>();
        CreateMap<TimingDto, Timing>();
        CreateMap<ProtocolLimitsDto, ProtocolLimits>();
        CreateMap<OptionalParameterDto, OptionalParameter>();
        CreateMap<FactsheetActionParameterDto, FactsheetActionParameter>();
        CreateMap<FactsheetActionDto, FactsheetAction>();
        CreateMap<ProtocolFeaturesDto, ProtocolFeatures>();
        CreateMap<WheelPositionDto, WheelPosition>();
        CreateMap<WheelDefinitionDto, WheelDefinition>();
        CreateMap<PolygonPointDto, PolygonPoint>();
        CreateMap<Envelope2dDto, Envelope2d>();
        CreateMap<Envelope3dDto, Envelope3d>();
        CreateMap<AgvGeometryDto, AgvGeometry>();
        CreateMap<BoundingBoxReferenceDto, BoundingBoxReference>();
        CreateMap<LoadDimensionsDto, LoadDimensions>();
        CreateMap<LoadSetDto, LoadSet>();
        CreateMap<LoadSpecificationDto, LoadSpecification>();
        CreateMap<VehicleVersionDto, VehicleVersion>();
        CreateMap<NetworkConfigDto, NetworkConfig>();
        CreateMap<VehicleConfigDto, VehicleConfig>();
        CreateMap<UploadFactsheetDto, Factsheet>();
    }
}
