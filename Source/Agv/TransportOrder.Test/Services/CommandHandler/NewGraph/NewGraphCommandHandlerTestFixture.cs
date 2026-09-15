using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.NewGraph;

internal class NewGraphCommandHandlerTestFixture : MassTransitConsumerTestFixture<NewGraphCommandHandler>
{
    private readonly Mock<INewGraphRepository> _repositoryMock = new();
    private readonly List<GraphSegment> _savedGraphSegments = [];

    public AddNewGraphSegment AddNewGraphSegment { get; } = new(Guid.NewGuid(), NewGraphGenerator.GenerateGraph());

    public IReadOnlyCollection<GraphSegment> SavedGraphSegments => _savedGraphSegments;

    public NewGraphCommandHandlerTestFixture()
    {
        _repositoryMock.Setup(r => r.AddOrUpdateGraphAsync(It.IsAny<GraphSegment>()))
            .Callback<GraphSegment>(graphSegment => _savedGraphSegments.Add(graphSegment))
            .Returns(Task.CompletedTask);

        AddServices(s => s.AddSingleton(_repositoryMock.Object));
        Build();
    }

    public NewGraphCommandHandlerTestFixture WithFailingRepository()
    {
        _repositoryMock.Setup(r => r.AddOrUpdateGraphAsync(It.IsAny<GraphSegment>()))
            .ThrowsAsync(new DbUpdateException());

        return this;
    }
}