using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Profiles;

/// <summary>
/// Provides mapping for Lifs.
/// </summary>
[UsedImplicitly]
public class LifProfile : Profile
{
    /// <inheritdoc/>
    public LifProfile()
    {
        CreateMap<LifFile, FileMetaInformationDto>()
            .ForMember(dto => dto.Id, opt => opt.MapFrom(l => l.Id))
            .ForMember(dto => dto.UploadTimestamp, opt => opt.MapFrom(l => l.MetaInformation.UploadTimestamp))
            .ForMember(dto => dto.Filename, opt => opt.MapFrom(l => l.MetaInformation.Filename))
            .ForMember(dto => dto.FileSizeMb, opt => opt.MapFrom(l => l.MetaInformation.FileSizeMb))
            .ForMember(dto => dto.UploadedBy, opt => opt.MapFrom(l => l.MetaInformation.UploadedBy))
            .ForMember(dto => dto.SupportedVehicleTypes, opt => opt.MapFrom(l => l.SupportedVehicleTypes))
            .ForMember(dto => dto.Creator, opt => opt.MapFrom(l => l.LifCollectionMetaInformation.Creator))
            .ForMember(dto => dto.ExportTimestamp, opt => opt.MapFrom(l => l.LifCollectionMetaInformation.ExportTimestamp))
            .ForMember(dto => dto.LifVersion, opt => opt.MapFrom(l => l.LifCollectionMetaInformation.LifVersion))
            .ForMember(dto => dto.ProjectIdentification, opt => opt.MapFrom(l => l.LifCollectionMetaInformation.ProjectIdentification));
    }
}