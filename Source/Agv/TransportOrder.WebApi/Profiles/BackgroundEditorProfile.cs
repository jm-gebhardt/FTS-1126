using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles.Resolver;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// AutoMapper profile for the background editor.
/// </summary>
public class BackgroundEditorProfile : WebApiProfile
{
    /// <inheritdoc cref="BackgroundEditorProfile"/>
    public BackgroundEditorProfile()
    {
        // Root document — delegates to the converters for both directions
        CreateMap<BackgroundDto, Background>().ConvertUsing<BackgroundDtoToBackgroundConverter>();
        CreateMap<Background, BackgroundDto>().ConvertUsing<BackgroundToBackgroundDtoConverter>();

        // Polymorphic domain → DTO mapping.
        // Include<> registers each concrete pair so AutoMapper selects the right map at runtime
        // when mapping from ICollection<BackgroundObject> inside BackgroundToBackgroundDtoConverter.
        CreateMap<BackgroundObject, BackgroundObjectDto>()
            .Include<RectangleObject, RectangleObjectDto>()
            .Include<TriangleObject,  TriangleObjectDto>()
            .Include<CircleObject,    CircleObjectDto>()
            .Include<LineObject,      LineObjectDto>()
            .Include<PolylineObject,  PolylineObjectDto>()
            .Include<TextObject,      TextObjectDto>()
            .ForMember(d => d.Id,         opt => opt.MapFrom(s => s.ExternalId))
            .ForMember(d => d.ObjectType, opt => opt.Ignore()); // Computed property on each DTO subtype

        CreateMap<RectangleObject, RectangleObjectDto>()
            .ForMember(d => d.Id,         opt => opt.MapFrom(s => s.ExternalId))
            .ForMember(d => d.ObjectType, opt => opt.Ignore());

        CreateMap<TriangleObject, TriangleObjectDto>()
            .ForMember(d => d.Id,         opt => opt.MapFrom(s => s.ExternalId))
            .ForMember(d => d.ObjectType, opt => opt.Ignore());

        CreateMap<CircleObject, CircleObjectDto>()
            .ForMember(d => d.Id,         opt => opt.MapFrom(s => s.ExternalId))
            .ForMember(d => d.ObjectType, opt => opt.Ignore());

        CreateMap<LineObject, LineObjectDto>()
            .ForMember(d => d.Id,         opt => opt.MapFrom(s => s.ExternalId))
            .ForMember(d => d.ObjectType, opt => opt.Ignore())
            .ForMember(d => d.LineStyle,  opt => opt.MapFrom(s => (LineStyleDto)s.LineStyle))
            .ForMember(d => d.Points,     opt => opt.MapFrom(s =>
                s.Points.OrderBy(p => p.Index).Select(p => new PointDto(p.X, p.Y)).ToList()));

        CreateMap<PolylineObject, PolylineObjectDto>()
            .ForMember(d => d.Id,         opt => opt.MapFrom(s => s.ExternalId))
            .ForMember(d => d.ObjectType, opt => opt.Ignore())
            .ForMember(d => d.LineStyle,  opt => opt.MapFrom(s => (LineStyleDto)s.LineStyle))
            .ForMember(d => d.Points,     opt => opt.MapFrom(s =>
                s.Points.OrderBy(p => p.Index).Select(p => new PointDto(p.X, p.Y)).ToList()));

        CreateMap<TextObject, TextObjectDto>()
            .ForMember(d => d.Id,         opt => opt.MapFrom(s => s.ExternalId))
            .ForMember(d => d.ObjectType, opt => opt.Ignore());
    }
}
