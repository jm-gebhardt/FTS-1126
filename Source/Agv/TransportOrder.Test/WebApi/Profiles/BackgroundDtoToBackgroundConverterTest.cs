using AutoMapper;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class BackgroundDtoToBackgroundConverterTest
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mapper = MappingHelper.GetMapper(new BackgroundEditorProfile());
    }

    [TestMethod]
    public void AssertConfigurationIsValid_DoesNotThrowException_WhenMappingProfileIsDefined()
    {
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [TestMethod]
    public void Map_BackgroundDtoToBackground_MapsRootPropertiesCorrectly()
    {
        var source = BackgroundGenerator.Generate();
        var dto = _mapper.Map<BackgroundDto>(source);

        var background = _mapper.Map<Background>(dto);

        background.ShouldNotBeNull();
        background.Id.ShouldBe(source.Id);
        background.Name.ShouldBe(source.Name);
        background.MapId.ShouldBe(source.MapId);
    }

    [TestMethod]
    public void Map_BackgroundDtoToBackground_InjectsBackgroundIdIntoAllObjects()
    {
        var source = BackgroundGenerator.Generate();
        var dto = _mapper.Map<BackgroundDto>(source);

        var background = _mapper.Map<Background>(dto);

        background.Objects.ShouldAllBe(o => o.BackgroundId == background.Id);
    }

    [TestMethod]
    public void Map_BackgroundDtoToBackground_MapsAllObjectsCorrectly()
    {
        var source = BackgroundGenerator.Generate();
        var dto = _mapper.Map<BackgroundDto>(source);

        var background = _mapper.Map<Background>(dto);

        background.Objects.Count.ShouldBe(source.Objects.Count);
        foreach (var obj in source.Objects)
        {
            background.Objects.ShouldContain(o => o.ExternalId == obj.ExternalId);
        }
    }

    [TestMethod]
    public void Map_BackgroundDtoToBackground_MapsLayersCorrectly()
    {
        var source = BackgroundGenerator.Generate();
        var dto = _mapper.Map<BackgroundDto>(source);

        var background = _mapper.Map<Background>(dto);

        background.Layers.Count.ShouldBe(source.Layers.Count);
        foreach (var layer in source.Layers)
        {
            var mappedLayer = background.Layers.FirstOrDefault(l => l.ExternalId == layer.ExternalId);
            mappedLayer.ShouldNotBeNull();
            mappedLayer.Name.ShouldBe(layer.Name);
            mappedLayer.Visible.ShouldBe(layer.Visible);
            mappedLayer.Locked.ShouldBe(layer.Locked);
        }
    }

    [TestMethod]
    public void Map_BackgroundDtoToBackground_FlattensColorPaletteIntoColorEntries()
    {
        var source = BackgroundGenerator.Generate();
        var dto = _mapper.Map<BackgroundDto>(source);

        var background = _mapper.Map<Background>(dto);

        List<int> lineColors = background.ColorEntries
            .Where(c => c.PaletteType == ColorPaletteType.Line)
            .Select(c => c.Value)
            .ToList();
        List<int> fillColors = background.ColorEntries
            .Where(c => c.PaletteType == ColorPaletteType.Fill)
            .Select(c => c.Value)
            .ToList();

        lineColors.ShouldBe(dto.Colors.LineColor);
        fillColors.ShouldBe(dto.Colors.FillColor);
    }

    [TestMethod]
    public void Map_BackgroundDtoToBackground_ResolvesLayerObjectReferences_WhenLayerObjectIdsMatchObjects()
    {
        var source = BackgroundGenerator.Generate();
        var dto = _mapper.Map<BackgroundDto>(source);
        int firstObjectId = dto.Objects.First().Id;
        var dtoWithObjectInLayer = dto with
        {
            Layers = dto.Layers
                .Select((l, i) => i == 0 ? l with { ObjectIds = [firstObjectId] } : l)
                .ToList()
        };

        var background = _mapper.Map<Background>(dtoWithObjectInLayer);

        background.Layers.First().Objects.Count.ShouldBe(1);
        background.Layers.First().Objects.First().ExternalId.ShouldBe(firstObjectId);
    }
}
