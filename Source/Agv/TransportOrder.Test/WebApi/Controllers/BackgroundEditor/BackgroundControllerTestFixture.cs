using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Gebhardt.Core.Observability.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.BackgroundEditor;

public class BackgroundControllerTestFixture : MockRequestClientControllerTestFixture<BackgroundController>
{
    protected override IEnumerable<Profile> GetProfiles() => [new BackgroundEditorProfile()];

    private GotBackground _gotBackground = null!;
    private GotBackgrounds _gotBackgrounds = new([]);
    private BackgroundAddedOrUpdated _backgroundAddedOrUpdated = new(BackgroundGenerator.Generate());

    protected override void SetupController()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => new Mock<ITracing>().Object);
        });

        AddRequestClient<AddOrUpdateBackground, BackgroundAddedOrUpdated>(() => _backgroundAddedOrUpdated);
        AddRequestClient<GetBackground, GotBackground>(() => _gotBackground);
        AddRequestClient<DeleteBackgroundScene, Success>(() => new Success());
        AddRequestClient<GetBackgrounds, GotBackgrounds>(() => _gotBackgrounds);
    }

    public new BackgroundControllerTestFixture Build()
    {
        base.Build();
        return this;
    }

    public BackgroundControllerTestFixture WithBackground(Background background)
    {
        _gotBackground = new GotBackground(background);
        _backgroundAddedOrUpdated = new BackgroundAddedOrUpdated(background);
        _gotBackgrounds = new GotBackgrounds([background]);
        return this;
    }

    public BackgroundControllerTestFixture WithBackgrounds(ICollection<Background> backgrounds)
    {
        _gotBackgrounds = new GotBackgrounds(backgrounds);
        return this;
    }
}
