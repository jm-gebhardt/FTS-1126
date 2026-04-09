using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles.Resolver;

/// <summary>
/// Converts a <see cref="BackgroundDto"/> to a <see cref="Background"/> domain object.
/// Handles three concerns that standard AutoMapper member mapping cannot resolve on its own:
/// <list type="bullet">
///   <item>Injecting the new <see cref="Background.Id"/> as <c>BackgroundId</c> into every child object.</item>
///   <item>Resolving layer object references from external ids to domain instances.</item>
///   <item>Flattening the two colour lists in <see cref="ColorPaletteDto"/> into <see cref="BackgroundColorEntry"/> rows.</item>
/// </list>
/// </summary>
[UsedImplicitly]
public class BackgroundDtoToBackgroundConverter : ITypeConverter<BackgroundDto, Background>
{
    /// <inheritdoc />
    public Background Convert(BackgroundDto source, Background destination, ResolutionContext context)
    {
        var background = new Background{Name = source.Name, MapId = source.MapId, Id = source.Id};

        List<BackgroundObject> objects = source.Objects.Select(dto => MapObject(dto, background.Id)).ToList();
        foreach (var obj in objects)
        {
            background.Objects.Add(obj);
        }

        Dictionary<int, BackgroundObject> objectsByExternalId = objects.ToDictionary(o => o.ExternalId);
        foreach (var layerDto in source.Layers)
        {
            var layer = new BackgroundLayer(background.Id, layerDto.Id, layerDto.Name, layerDto.Visible, layerDto.Locked);
            foreach (int externalId in layerDto.ObjectIds)
            {
                if (objectsByExternalId.TryGetValue(externalId, out var obj))
                {
                    layer.Objects.Add(obj);
                }
            }
            background.Layers.Add(layer);
        }

        foreach (int value in source.Colors.LineColor)
        {
            background.ColorEntries.Add(new BackgroundColorEntry(background.Id, ColorPaletteType.Line, value));
        }
        foreach (int value in source.Colors.FillColor)
        {
            background.ColorEntries.Add(new BackgroundColorEntry(background.Id, ColorPaletteType.Fill, value));
        }

        return background;
    }

    private static BackgroundObject MapObject(BackgroundObjectDto dto, Guid backgroundId) => dto switch
    {
        RectangleObjectDto r => new RectangleObject(
            backgroundId, r.Id, r.X, r.Y, r.Width, r.Height, r.Rotation, r.ScaleX, r.ScaleY,
            r.Color, r.BorderWidth, r.BorderColor),

        TriangleObjectDto t => new TriangleObject(
            backgroundId, t.Id, t.X, t.Y, t.Width, t.Height, t.Rotation, t.ScaleX, t.ScaleY,
            t.Color, t.BorderWidth, t.BorderColor),

        CircleObjectDto c => new CircleObject(
            backgroundId, c.Id, c.X, c.Y, c.Width, c.Height, c.Rotation, c.ScaleX, c.ScaleY,
            c.Color, c.BorderWidth, c.BorderColor),

        LineObjectDto l => MapLine(l, backgroundId),

        PolylineObjectDto p => MapPolyline(p, backgroundId),

        TextObjectDto t => new TextObject(
            backgroundId, t.Id, t.X, t.Y, t.Width, t.Height, t.Rotation, t.ScaleX, t.ScaleY,
            t.Color, t.Text, t.FontSize, t.FontFamily),

        _ => throw new ArgumentException($"Unhandled {nameof(BackgroundObjectDto)} type: {dto.GetType().Name}.")
    };

    private static LineObject MapLine(LineObjectDto dto, Guid backgroundId)
    {
        var line = new LineObject(
            backgroundId, dto.Id, dto.X, dto.Y, dto.Width, dto.Height, dto.Rotation, dto.ScaleX, dto.ScaleY,
            dto.Color, dto.LineWidth, (LineStyle)dto.LineStyle);

        foreach ((var point, int index) in dto.Points.Select((p, i) => (p, i)))
        {
            line.Points.Add(new BackgroundObjectPoint(line.Id, index, point.X, point.Y));
        }

        return line;
    }

    private static PolylineObject MapPolyline(PolylineObjectDto dto, Guid backgroundId)
    {
        var polyline = new PolylineObject(
            backgroundId, dto.Id, dto.X, dto.Y, dto.Width, dto.Height, dto.Rotation, dto.ScaleX, dto.ScaleY,
            dto.Color, dto.LineWidth, (LineStyle)dto.LineStyle);

        foreach ((var point, int index) in dto.Points.Select((p, i) => (p, i)))
        {
            polyline.Points.Add(new BackgroundObjectPoint(polyline.Id, index, point.X, point.Y));
        }

        return polyline;
    }
}
