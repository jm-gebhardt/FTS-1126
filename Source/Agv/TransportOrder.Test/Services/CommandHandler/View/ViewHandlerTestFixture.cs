using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Views;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SkiaSharp;
using DomainView = Gebhardt.Acc.TransportOrder.Domain.Models.Views.View;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.View;

public class ViewHandlerTestFixture : MediatorTestFixture<GetViews, ViewHandler, MockConsumer>
{
    private const string StoredViewName = "Hall";
    public const string LevelId = "1";
    public const string NewViewName = "New hall";
    public const double Resolution = 0.05;
    public const double XOffset = 1;
    public const double YOffset = 2;

    public static readonly Guid UnknownViewId = Guid.NewGuid();

    private readonly Mock<IViewRepository> _viewRepository = new();
    private readonly Mock<INewGraphRepository> _graphRepository = new();
    private readonly Dictionary<Guid, DomainView> _storedViews = [];
    private readonly Dictionary<Guid, byte[]> _storedImages = [];
    private readonly List<LevelArea> _levelAreas = [];

    public static ViewArea Area { get; } = new(0, 0, 10, 5);

    public static byte[] PngImage { get; } = EncodeImage(SKEncodedImageFormat.Png);

    public static byte[] JpegImage { get; } = EncodeImage(SKEncodedImageFormat.Jpeg);

    public IReadOnlyDictionary<Guid, DomainView> StoredViews => _storedViews;

    public IReadOnlyDictionary<Guid, byte[]> StoredImages => _storedImages;

    public ViewHandlerTestFixture()
    {
        _viewRepository.Setup(r => r.GetViewsAsync()).ReturnsAsync(() => _storedViews.Values.OrderBy(v => v.Name).ToList());
        _viewRepository.Setup(r => r.FindViewAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => _storedViews.GetValueOrDefault(id));
        _viewRepository.Setup(r => r.AddOrUpdateViewAsync(It.IsAny<DomainView>(), It.IsAny<byte[]?>()))
            .Callback<DomainView, byte[]?>(StoreView)
            .Returns(Task.CompletedTask);
        _viewRepository.Setup(r => r.DeleteViewAsync(It.IsAny<Guid>()))
            .Callback<Guid>(id => _storedViews.Remove(id))
            .Returns(Task.CompletedTask);
        _viewRepository.Setup(r => r.GetBackgroundImageAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => _storedImages.GetValueOrDefault(id));
        _graphRepository.Setup(r => r.GetLevelAreasAsync()).ReturnsAsync(() => _levelAreas);

        AddServices(s =>
        {
            s.AddTransient(_ => _viewRepository.Object);
            s.AddTransient(_ => _graphRepository.Object);
        });
        base.Build();
    }

    private static byte[] EncodeImage(SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(4, 4);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }

    private void StoreView(DomainView view, byte[]? backgroundImage)
    {
        _storedViews[view.Id] = view;
        if (backgroundImage is not null)
        {
            _storedImages[view.Id] = backgroundImage;
        }
    }

    protected override IEnumerable<Profile> GetProfiles()
    {
        return [new ViewProfile()];
    }

    public ViewHandlerTestFixture WithLevel()
    {
        _levelAreas.Add(new LevelArea(LevelId, Area));
        return this;
    }

    public ViewHandlerTestFixture WithStoredView(out DomainView view)
    {
        view = new DomainView(Guid.NewGuid(), StoredViewName, LevelId, isDefault: true, Area);
        _storedViews[view.Id] = view;
        return this;
    }

    public DomainView GetGeneratedView()
    {
        return DomainView.CreateGenerated(_levelAreas).First();
    }
}