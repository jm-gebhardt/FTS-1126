using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Factsheets;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Profiles;

/// <summary>
/// Provides a profile for factsheet mapping.
/// </summary>
public class FactsheetProfile : Profile
{
    /// <inheritdoc/>
    public FactsheetProfile()
    {
        CreateMap<Factsheet, AgvData>()
            .ConstructUsing(f => new AgvData(
                f.SerialNumber.ToGuid(),
                f.TypeSpecification.SeriesName,
                $"{f.Manufacturer}.{f.TypeSpecification.SeriesName}",
                GetIp4Address(f),
                GetSoftwareVersion(f),
                1,
                f.PhysicalParameters.AccelerationMax,
                f.PhysicalParameters.SpeedMax,
                1000,
                0.0,
                1000,
                f.PhysicalParameters.Width,
                f.PhysicalParameters.Length,
                (long)f.TypeSpecification.MaxLoadMass,
                Array.Empty<AgvData.Sensor>()
            ))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<Factsheet, AgvType>()
            .ConstructUsing(f => new AgvType(
                $"{f.Manufacturer}.{f.TypeSpecification.SeriesName}",
                f.Manufacturer,
                f.TypeSpecification.SeriesName,
                f.PhysicalParameters.Length,
                f.PhysicalParameters.Width,
                string.Empty,
                true))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<AgvType, AgvTypeInfo>();

        CreateMap<FactsheetAction, AgvAction>()
            .ForMember(a => a.AgvSerialNumber, opt => opt.Ignore());

        CreateMap<FactsheetActionParameter, AgvActionParameter>()
            .ForMember(a => a.IsOptional, opt => opt.MapFrom(fa => fa.IsOptional ?? false));
    }

    private string GetIp4Address(Factsheet factsheet)
    {
        return factsheet.VehicleConfig?.Network?.LocalIpAddress ?? string.Empty;
    }

    private string GetSoftwareVersion(Factsheet factsheet)
    {
        return factsheet.VehicleConfig?.Versions != null ? factsheet.VehicleConfig!.Versions!.SingleOrDefault(v => string.Equals(v.Key, "softwareVersion", StringComparison.InvariantCultureIgnoreCase))?.Value ?? string.Empty : string.Empty;
    }
}