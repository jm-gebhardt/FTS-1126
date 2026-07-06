using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;
using MassTransit;
using Moq;
using Shouldly;
using AgvMap = Gebhardt.Acc.Common.Domain.Models.Graphs.Map;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Map;

[TestClass]
public class MapCommandHandlerTest
{
    [TestMethod]
    public async Task SetBackground_ShouldPersist_AndPublish_ChangedEvent()
    {
        var fixture = new MapRequestTestFixture<SetBackground, MapCommandHandler>();
        var correlationId = fixture.Maps[0].CorrelationId;
        var request = new SetBackground(
            correlationId,
            Resolution: 0.1,
            XOffset: 12,
            YOffset: 24,
            MapBackgroundGenerator.Image1
        );

        Response<Success> response = await fixture.GetResponseAsync<Success>(request);

        response.Message.ShouldBeOfType<Success>();
        fixture.MapRepositoryMock.Verify(r => r.SetCustomBackgroundAsync(correlationId, It.Is<MapBackground>(m => m.Width == 1)), Times.Once);
        await fixture.ShouldHavePublishedAsync<MapChanged>();
    }

    [TestMethod]
    public async Task SetBackground_ShouldThrow_ArgumentException_WhenImageInvalid()
    {
        var correlationId = Guid.NewGuid();
        byte[] invalidBytes = [0, 1];
        var fixture = new MapRequestTestFixture<SetBackground, MapCommandHandler>();

        await MediatorShould.ThrowRequestExceptionAsync<BitmapDecodingException>(async () => await fixture.GetResponseAsync<Success>(new SetBackground(correlationId, 1, 0, 0, invalidBytes)));
    }

    [TestMethod]
    public async Task DeleteBackground_ShouldPublish_ChangedEvent_WhenCustomBackgroundWasDeleted()
    {
        var fixture = new MapRequestTestFixture<DeleteBackground, MapCommandHandler>();
        var mapBackground = fixture.Maps[0];
        fixture.MapRepositoryMock.Setup(r => r.DeleteCustomBackgroundAsync(mapBackground.CorrelationId)).ReturnsAsync(true);

        await fixture.GetResponseAsync<Success>(new DeleteBackground(mapBackground.CorrelationId));

        await fixture.ShouldHavePublishedAsync<MapChanged>();
    }

    [TestMethod]
    public async Task DeleteBackground_ShouldNotPublish_WhenNoBackgroundExisted()
    {
        var mapBackground = MapBackgroundGenerator.Generate();
        MapRequestTestFixture<DeleteBackground, MapCommandHandler> fixture = new MapRequestTestFixture<DeleteBackground, MapCommandHandler>()
            .WithMapBackground(mapBackground);
        fixture.MapRepositoryMock.Setup(r => r.DeleteCustomBackgroundAsync(mapBackground.CorrelationId)).ReturnsAsync(false);

        await fixture.GetResponseAsync<Success>(new DeleteBackground(mapBackground.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<MapChanged>();
    }

    [TestMethod]
    public async Task GenerateDefaultMap_WhenMapDoesNotExist_CallsAddPureMapAsync()
    {
        var fixture = new MapCommandHandlerBusTestFixture();
        fixture.MapRepositoryMock.Setup(r => r.Exists(It.IsAny<Guid>())).Returns(false);

        await fixture.TestHarness.Bus.Publish(new GenerateDefaultMap());
        await fixture.ShouldHaveConsumedAsync<GenerateDefaultMap>();

        fixture.MapRepositoryMock.Verify(r => r.AddPureMapAsync(It.IsAny<TransportOrder.Domain.Models.Map.Map>()), Times.Once);
    }

    [TestMethod]
    public async Task GenerateDefaultMap_WhenMapAlreadyExists_DoesNotCallAddPureMapAsync()
    {
        var fixture = new MapCommandHandlerBusTestFixture();
        fixture.MapRepositoryMock.Setup(r => r.Exists(It.IsAny<Guid>())).Returns(true);

        await fixture.TestHarness.Bus.Publish(new GenerateDefaultMap());
        await fixture.ShouldHaveConsumedAsync<GenerateDefaultMap>();

        fixture.MapRepositoryMock.Verify(r => r.AddPureMapAsync(It.IsAny<TransportOrder.Domain.Models.Map.Map>()), Times.Never);
    }

    [TestMethod]
    public async Task SaveMap_WhenIncomingMapIsLarger_StoresCompositeImage()
    {
        // Default: Width=10, Height=20, Resolution=2, XOffset=0 → world X:[0,20], Y:[0,40]
        // Incoming: Width=15, Height=25, Resolution=2, XOffset=0 → world X:[0,30], Y:[0,50] → LARGER
        var fixture = SetUpSaveMapFixture(out var storedMapCapture,
            defaultWidth: 10, defaultHeight: 20, defaultResolution: 2,
            incomingWidth: 15, incomingHeight: 25, incomingResolution: 2);

        await fixture.TestHarness.Bus.Publish(new SaveMap(storedMapCapture.IncomingMap, Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<SaveMap>();

        byte[] expectedWhiteBytes = Convert.FromBase64String(MapHelper.GenerateWhitePngBase64(10, 20));
        storedMapCapture.StoredMap.ShouldNotBeNull();
        storedMapCapture.StoredMap!.BaseBackground!.Image.ShouldNotBe(expectedWhiteBytes);
    }

    [TestMethod]
    public async Task SaveMap_WhenIncomingImageExceedsNodeBounds_ExpandsCanvasToFitImage()
    {
        // Default: Width=10, Height=20, Resolution=1 → world [0,10]×[0,20]
        // Incoming image: Width=30, Height=40, Resolution=1 → world [0,30]×[0,40]
        // Union [0,30]×[0,40]; MapExtensions formula: margin=0
        var fixture = SetUpSaveMapFixture(out var storedMapCapture,
            defaultWidth: 10, defaultHeight: 20, defaultResolution: 1,
            incomingWidth: 30, incomingHeight: 40, incomingResolution: 1);

        await fixture.TestHarness.Bus.Publish(new SaveMap(storedMapCapture.IncomingMap, Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<SaveMap>();

        storedMapCapture.StoredMap.ShouldNotBeNull();
        storedMapCapture.StoredMap!.BaseBackground!.Width.ShouldBe(ExpectedExtendedDimension(rangeMeters: 30f, resolution: 1f, ignoreMargin: true));
        storedMapCapture.StoredMap!.BaseBackground!.Height.ShouldBe(ExpectedExtendedDimension(rangeMeters: 40f, resolution: 1f, ignoreMargin: true));
    }

    [TestMethod]
    public async Task SaveMap_WhenIncomingMapIsNotLarger_PreservesBackgroundWithCorrectDimensions()
    {
        // Default: Width=10, Height=20, Resolution=2, XOffset=0 → world X:[0,20], Y:[0,40]
        // Incoming: Width=5, Height=10, Resolution=2, XOffset=0 → world X:[0,10], Y:[0,20] → NOT LARGER
        // Union stays [0,20]×[0,40]; composite is placed on that extended canvas.
        var fixture = SetUpSaveMapFixture(out var storedMapCapture,
            defaultWidth: 10, defaultHeight: 20, defaultResolution: 2,
            incomingWidth: 5, incomingHeight: 10, incomingResolution: 2);

        await fixture.TestHarness.Bus.Publish(new SaveMap(storedMapCapture.IncomingMap, Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<SaveMap>();

        storedMapCapture.StoredMap.ShouldNotBeNull();
        storedMapCapture.StoredMap!.BaseBackground!.Width.ShouldBe(ExpectedExtendedDimension(rangeMeters: 20f, resolution: 2f, ignoreMargin: true));
        storedMapCapture.StoredMap!.BaseBackground!.Height.ShouldBe(ExpectedExtendedDimension(rangeMeters: 40f, resolution: 2f, ignoreMargin: true));
    }

    [TestMethod]
    public async Task SaveMap_WhenIncomingHasCoarserResolutionAndLargerArea_UsesCoarserResolutionAndCorrectCanvasSize()
    {
        // Default: fine resolution 0.005 m/px, 200×200px, xOffset=0 → world [0,1m]×[0,1m]
        // Incoming: coarse resolution 0.05 m/px, 100×100px, xOffset=0 → world [0,5m]×[0,5m]
        // outputResolution = max(0.05, 0.005) = 0.05 to avoid OOM at fine resolution over large area.
        const float outputResolution = 0.05f;
        var fixture = SetUpSaveMapFixture(out var storedMapCapture,
            defaultWidth: 200, defaultHeight: 200, defaultResolution: 0.005,
            incomingWidth: 100, incomingHeight: 100, incomingResolution: outputResolution);

        await fixture.TestHarness.Bus.Publish(new SaveMap(storedMapCapture.IncomingMap, Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<SaveMap>();

        storedMapCapture.StoredMap.ShouldNotBeNull();
        storedMapCapture.StoredMap!.BaseBackground!.Width.ShouldBe(ExpectedExtendedDimension(rangeMeters: 5f, resolution: outputResolution, ignoreMargin: true));
        storedMapCapture.StoredMap!.BaseBackground!.Height.ShouldBe(ExpectedExtendedDimension(rangeMeters: 5f, resolution: outputResolution, ignoreMargin: true));
    }

    /// <summary>
    /// Mirrors the MapExtensions.Extend formula so that test expectations stay in sync with production logic.
    /// </summary>
    private static int ExpectedExtendedDimension(float rangeMeters, float resolution, bool ignoreMargin=false)
    {
        float marginMeters = ignoreMargin? 0.0f : 2.0f;   // MapExtensions.MarginMeters
        float marginFraction = ignoreMargin ? 0.0f : 0.1f; // MapExtensions.MarginFraction
        float margin = Math.Max(marginMeters, rangeMeters * marginFraction);
        return (int)Math.Ceiling((rangeMeters + 2 * margin) / resolution);
    }

    private static MapCommandHandlerBusTestFixture SetUpSaveMapFixture(
        out SaveMapCapture capture,
        int defaultWidth, int defaultHeight, double defaultResolution,
        int incomingWidth, int incomingHeight, float incomingResolution)
    {
        var fixture = new MapCommandHandlerBusTestFixture();
        var defaultCorrelationId = TransportOrder.Domain.Models.Map.Map.DefaultMapId;

        var defaultMap = MapGenerator.Generate(
            correlationId: defaultCorrelationId,
            width: defaultWidth, height: defaultHeight, resolution: defaultResolution,
            xOffset: 0, yOffset: 0);
        fixture.MapRepositoryMock.Setup(r => r.GetMapAsync(defaultCorrelationId)).ReturnsAsync(defaultMap);

        var localCapture = new SaveMapCapture();
        fixture.MapRepositoryMock
            .Setup(r => r.UpdateDefaultMapLockedAsync(
                It.IsAny<Func<TransportOrder.Domain.Models.Map.Map, CancellationToken, Task<(TransportOrder.Domain.Models.Map.Map, Guid)?>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<TransportOrder.Domain.Models.Map.Map, CancellationToken, Task<(TransportOrder.Domain.Models.Map.Map, Guid)?>>, CancellationToken>(
                async (fn, ct) =>
                {
                    (TransportOrder.Domain.Models.Map.Map, Guid)? result = await fn(defaultMap, ct);
                    if (result is null)
                    {
                        return false;
                    }
                    localCapture.StoredMap = result.Value.Item1;
                    return true;
                });

        fixture.GraphRepositoryMock
            .Setup(r => r.GetAgvIdsRelatedToNodesAsync(It.IsAny<ICollection<GraphNode>>()))
            .ReturnsAsync(new List<Guid>());

        fixture.MapperMock.Setup(m => m.Map<List<GraphNode>>(It.IsAny<object>()))
            .Returns<object>(src => src is IEnumerable<GraphNode> nodes ? nodes.ToList() : []);
        fixture.MapperMock.Setup(m => m.Map<List<GraphEdge>>(It.IsAny<object>()))
            .Returns<object>(src => src is IEnumerable<GraphEdge> edges ? edges.ToList() : []);

        string incomingImageData = MapHelper.GenerateWhitePngBase64(incomingWidth, incomingHeight);
        localCapture.IncomingMap = new AgvMap(
            Guid.NewGuid(), "incoming", DateTime.UtcNow, incomingImageData,
            incomingWidth, incomingHeight, incomingResolution, xOffset: 0f, yOffset: 0f);

        capture = localCapture;
        return fixture;
    }

    private sealed class SaveMapCapture
    {
        public AgvMap IncomingMap { get; set; } = null!;

        public TransportOrder.Domain.Models.Map.Map? StoredMap { get; set; }
    }
}