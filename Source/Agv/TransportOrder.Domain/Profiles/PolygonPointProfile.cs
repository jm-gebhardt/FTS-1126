using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.TransportOrder.Domain.Profiles;

/// <summary>
/// Profile for PolygonPoint.
/// </summary>
public class PolygonPointProfile : Profile
{
    /// <summary/>
    public PolygonPointProfile()
    {
         CreateMap<Models.Polygon.PolygonPoint, PolygonPoint>()
            .ForSourceMember(s => s.LocationId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Id, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Index, opt => opt.DoNotValidate());
        CreateMap<PolygonPoint, Models.Polygon.PolygonPoint>()
            .ForMember(s => s.LocationId, opt => opt.Ignore())
            .ForMember(s => s.Id, opt => opt.Ignore())
            .ForMember(s => s.Index, opt => opt.Ignore());
        CreateMap<List<PolygonPoint>, List<Models.Polygon.PolygonPoint>>()
            .ConvertUsing((src, _, context) =>
            {
                return src.Select((commonPolygonPoint, index) =>
                {
                    var point = context.Mapper.Map<Models.Polygon.PolygonPoint>(commonPolygonPoint);
                    point.Index = index;
                    return point;
                }).ToList();
            });
        CreateMap<ICollection<PolygonPoint>, ICollection<Models.Polygon.PolygonPoint>>()
            .ConvertUsing((src, _, context) =>
            {
                return src.Select((commonPolygonPoint, index) =>
                {
                    var point = context.Mapper.Map<Models.Polygon.PolygonPoint>(commonPolygonPoint);
                    point.Index = index;
                    return point;
                }).ToList();
            });
    }
}