using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DomainAgvGraph = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.AgvGraph;
using CommonAgvGraph = Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

public class PublishAgvGraphHandlerTestFixture : MassTransitConsumerTestFixture<PublishAgvGraphHandler>
{
    private readonly Mock<IAgvGraphProvider> _agvGraphProvider = new();
    private readonly Mock<ILifGraphConverter> _lifGraphConverter = new();

    public PublishAgvGraphHandlerTestFixture()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _agvGraphProvider.Object);
            sc.AddTransient(_ => _lifGraphConverter.Object);
            sc.AddSingleton<TimeProvider>(_ => TimeProvider);
        });
        Build();
    }

    public PublishAgvGraphHandlerTestFixture SetDomainGraph(Guid agvCorrelationId, DomainAgvGraph domainGraph, CommonAgvGraph commonGraph)
    {
        _agvGraphProvider.Setup(x => x.GetAgvGraphAsync(agvCorrelationId)).ReturnsAsync(domainGraph);
        _lifGraphConverter.Setup(x => x.ConvertToCommonAgvGraph(domainGraph)).Returns(commonGraph);
        return this;
    }

    public PublishAgvGraphHandlerTestFixture SetNoDomainGraph(Guid agvCorrelationId)
    {
        _agvGraphProvider.Setup(x => x.GetAgvGraphAsync(agvCorrelationId)).ReturnsAsync((DomainAgvGraph?)null);
        return this;
    }
}