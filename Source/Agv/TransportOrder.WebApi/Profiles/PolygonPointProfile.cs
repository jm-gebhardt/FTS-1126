using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for polygon points
/// </summary>
public class PolygonPointProfile: WebApiProfile
{
    /// <inheritdoc cref="PolygonPointProfile"/>>
    public PolygonPointProfile()
    {
        CreateMap<TriggerPolygonPoint, PolygonPointDto>()
            .ForSourceMember(s => s.LocationId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Id, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Index, opt => opt.DoNotValidate());
        CreateMap<PolygonPointDto, TriggerPolygonPoint>()
            .ForMember(s => s.LocationId, opt => opt.Ignore())
            .ForMember(s => s.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Index, opt => opt.Ignore());
        CreateMap<List<PolygonPointDto>, List<TriggerPolygonPoint>>()
            .ConvertUsing((src, _, context) =>
            {
                return src.Select((dto, index) =>
                {
                    var point = context.Mapper.Map<TriggerPolygonPoint>(dto);
                    point.Index = index;
                    return point;
                }).ToList();
            });
        CreateMap<ICollection<PolygonPointDto>, ICollection<TriggerPolygonPoint>>()
            .ConvertUsing((src, _, context) =>
            {
                return src.Select((dto, index) =>
                {
                    var point = context.Mapper.Map<TriggerPolygonPoint>(dto);
                    point.Index = index;
                    return point;
                }).ToList();
            });

        CreateMap<RestrictedAreaPolygonPoint, PolygonPointDto>()
            .ForSourceMember(s => s.LocationId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Id, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Index, opt => opt.DoNotValidate());
        CreateMap<PolygonPointDto, RestrictedAreaPolygonPoint>()
            .ForMember(s => s.LocationId, opt => opt.Ignore())
            .ForMember(s => s.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Index, opt => opt.Ignore());
        CreateMap<List<PolygonPointDto>, List<RestrictedAreaPolygonPoint>>()
            .ConvertUsing((src, _, context) =>
            {
                return src.Select((dto, index) =>
                {
                    var point = context.Mapper.Map<RestrictedAreaPolygonPoint>(dto);
                    point.Index = index;
                    return point;
                }).ToList();
            });
        CreateMap<ICollection<PolygonPointDto>, ICollection<RestrictedAreaPolygonPoint>>()
            .ConvertUsing((src, _, context) =>
            {
                return src.Select((dto, index) =>
                {
                    var point = context.Mapper.Map<RestrictedAreaPolygonPoint>(dto);
                    point.Index = index;
                    return point;
                }).ToList();
            });

        CreateMap<PolygonPoint, PolygonPointDto>()
            .ForSourceMember(s => s.LocationId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Id, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Index, opt => opt.DoNotValidate());
        CreateMap<PolygonPointDto, PolygonPoint>()
            .ForMember(s => s.LocationId, opt => opt.Ignore())
            .ForMember(s => s.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Index, opt => opt.Ignore());
        CreateMap<List<PolygonPointDto>, List<PolygonPoint>>()
            .ConvertUsing((src, _, context) =>
            {
                return src.Select((dto, index) =>
                {
                    var point = context.Mapper.Map<PolygonPoint>(dto);
                    point.Index = index;
                    return point;
                }).ToList();
            });
        CreateMap<ICollection<PolygonPointDto>, ICollection<PolygonPoint>>()
            .ConvertUsing((src, _, context) =>
            {
                return src.Select((dto, index) =>
                {
                    var point = context.Mapper.Map<PolygonPoint>(dto);
                    point.Index = index;
                    return point;
                }).ToList();
            });
    }
}