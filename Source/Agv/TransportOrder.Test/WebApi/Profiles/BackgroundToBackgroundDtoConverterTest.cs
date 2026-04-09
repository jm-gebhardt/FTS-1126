using AutoMapper;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class BackgroundToBackgroundDtoConverterTest
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
    public void Map_BackgroundToBackgroundDto_MapsRootPropertiesCorrectly()
    {
        var background = BackgroundGenerator.Generate();

        var dto = _mapper.Map<BackgroundDto>(background);

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(background.Id);
        dto.Name.ShouldBe(background.Name);
        dto.MapId.ShouldBe(background.MapId);
    }

    [TestMethod]
    public void Map_BackgroundToBackgroundDto_MapsAllObjectsCorrectly()
    {
        var background = BackgroundGenerator.Generate();

        var dto = _mapper.Map<BackgroundDto>(background);

        dto.Objects.Count.ShouldBe(background.Objects.Count);
        foreach (var obj in background.Objects)
        {
            dto.Objects.ShouldContain(o => o.Id == obj.ExternalId);
        }
    }

    [TestMethod]
    public void Map_BackgroundToBackgroundDto_MapsLayersCorrectly()
    {
        var background = BackgroundGenerator.Generate();

        var dto = _mapper.Map<BackgroundDto>(background);

        dto.Layers.Count.ShouldBe(background.Layers.Count);
        foreach (var layer in background.Layers)
        {
            var layerDto = dto.Layers.FirstOrDefault(l => l.Id == layer.ExternalId);
            layerDto.ShouldNotBeNull();
            layerDto.Name.ShouldBe(layer.Name);
            layerDto.Visible.ShouldBe(layer.Visible);
            layerDto.Locked.ShouldBe(layer.Locked);
            layerDto.ObjectIds.ShouldBe(layer.Objects.Select(o => o.ExternalId).ToList());
        }
    }

    [TestMethod]
    public void Map_BackgroundToBackgroundDto_SplitsColorEntriesIntoLineAndFillColors()
    {
        var background = BackgroundGenerator.Generate();

        var dto = _mapper.Map<BackgroundDto>(background);

        List<int> expectedLineColors = background.ColorEntries
            .Where(c => c.PaletteType == ColorPaletteType.Line)
            .Select(c => c.Value)
            .ToList();
        List<int> expectedFillColors = background.ColorEntries
            .Where(c => c.PaletteType == ColorPaletteType.Fill)
            .Select(c => c.Value)
            .ToList();

        dto.Colors.LineColor.ShouldBe(expectedLineColors);
        dto.Colors.FillColor.ShouldBe(expectedFillColors);
    }
}
