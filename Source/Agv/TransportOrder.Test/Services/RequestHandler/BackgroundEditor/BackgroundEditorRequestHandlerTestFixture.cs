using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Core.Observability.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.BackgroundEditor;

public class BackgroundEditorRequestHandlerTestFixture<TRequest> : MediatorTestFixture<TRequest, BackgroundEditorRequestHandler, MockConsumer>
    where TRequest : class
{
    private readonly Mock<IBackgroundEditorRepository> _backgroundEditorRepository = new();

    public BackgroundEditorRequestHandlerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _backgroundEditorRepository.Object);
            sc.AddTransient(_ => new Mock<ILoggingService>().Object);
            sc.AddTransient(_ => new Mock<ITracing>().Object);
        });
        base.Build();
    }

    public BackgroundEditorRequestHandlerTestFixture<TRequest> WithBackground(Background background)
    {
        _backgroundEditorRepository.Setup(x => x.AddOrUpdateBackgroundAsync(It.IsAny<Background>()));
        _backgroundEditorRepository.Setup(x => x.GetBackgroundsAsync(It.IsAny<ICollection<Guid>>(), It.IsAny<int>())).ReturnsAsync([background]);
        _backgroundEditorRepository.Setup(x => x.GetRequiredBackgroundAsync(It.IsAny<Guid>())).ReturnsAsync(background);
        _backgroundEditorRepository.Setup(x => x.DeleteBackgroundAsync(It.IsAny<Guid>()));
        return this;
    }
}