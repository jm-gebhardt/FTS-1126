using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DomainGraph = Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.NewGraph;

internal class NewGraphCommandHandlerTestFixture : MassTransitConsumerTestFixture<NewGraphCommandHandler>
{
    private readonly Mock<INewGraphRepository> _repositoryMock = new();
    private readonly List<DomainGraph> _savedGraphs = [];

    public AddNewGraph AddNewGraph { get; } = new(Guid.NewGuid(), NewGraphGenerator.GenerateGraph());

    public IReadOnlyCollection<DomainGraph> SavedGraphs => _savedGraphs;

    public NewGraphCommandHandlerTestFixture()
    {
        _repositoryMock.Setup(r => r.AddOrUpdateGraphAsync(It.IsAny<DomainGraph>()))
            .Callback<DomainGraph>(graph => _savedGraphs.Add(graph))
            .Returns(Task.CompletedTask);

        AddServices(s => s.AddSingleton(_repositoryMock.Object));
        Build();
    }

    public NewGraphCommandHandlerTestFixture WithFailingRepository()
    {
        _repositoryMock.Setup(r => r.AddOrUpdateGraphAsync(It.IsAny<DomainGraph>()))
            .ThrowsAsync(new DbUpdateException());

        return this;
    }
}