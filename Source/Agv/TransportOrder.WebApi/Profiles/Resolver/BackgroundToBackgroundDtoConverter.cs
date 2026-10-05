using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles.Resolver;

/// <summary>
/// Converts a <see cref="Background"/> domain object to a <see cref="BackgroundDto"/>.
/// object collection, and re-groups <see cref="BackgroundColorEntry"/> rows back into
/// the two-list <see cref="ColorPaletteDto"/> shape.
/// </summary>
[UsedImplicitly]
public class BackgroundToBackgroundDtoConverter : ITypeConverter<Background, BackgroundDto>
{
    /// <inheritdoc />
    public BackgroundDto Convert(Background source, BackgroundDto destination, ResolutionContext context)
    {
        var objects = source.Objects
            .Select(o => context.Mapper.Map<BackgroundObjectDto>(o))
            .ToList();

        var layers = source.Layers
            .Select(l => new LayerDto(
                l.ExternalId,
                l.Name,
                l.Visible,
                l.Locked,
                l.Objects.Select(o => o.ExternalId).ToList()))
            .ToList();

        var colors = new ColorPaletteDto(
            source.ColorEntries
                .Where(c => c.PaletteType == ColorPaletteType.Line)
                .Select(c => c.Value)
                .ToList(),
            source.ColorEntries
                .Where(c => c.PaletteType == ColorPaletteType.Fill)
                .Select(c => c.Value)
                .ToList());

        return new BackgroundDto(objects, layers, colors, source.MapId, source.Name, source.Id);
    }
}
