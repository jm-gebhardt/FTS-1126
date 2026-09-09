using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

public class LifCommandHandlerTestFixture : MassTransitConsumerTestFixture<LifCommandHandler>
{
    private static readonly DateTime _uploadTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILifRepository> _lifRepository = new();
    private readonly Mock<IAgvTypeRepository> _agvTypeRepository = new();
    private readonly List<LifFile> _storedFiles = [];
    private readonly List<Layout> _storedLayouts = [];

    private LifLayoutCollection _layoutCollection = NewGraphGenerator.GenerateCollection(NewGraphGenerator.GenerateLayout());

    public StoreLif StoreLif => field ??= new StoreLif(
        NewGraphGenerator.GenerateLifFile(_uploadTimestamp, _layoutCollection.GetSupportedVehicleTypes(), _layoutCollection.Layouts.Select(l => l.Id).ToList()),
        _layoutCollection);

    public LifCommandHandlerTestFixture()
    {
        _lifRepository.Setup(r => r.GetLifsByVehicleTypeIdsAsync(It.IsAny<string[]>()))
            .ReturnsAsync(() => ((ICollection<LifFile>)_storedFiles, (ICollection<Layout>)_storedLayouts));

        _agvTypeRepository.Setup(r => r.GetAgvTypeIdentifiersAsync())
            .ReturnsAsync(NewGraphGenerator.AgvTypeIdentifiers);

        AddServices(s =>
        {
            s.AddSingleton(_lifRepository.Object);
            s.AddSingleton(_agvTypeRepository.Object);
            s.AddSingleton(Mock.Of<INurbsEdgeBuilder>());
        });
        Build();
    }

    public LifCommandHandlerTestFixture WithLayouts(params Layout[] layouts)
    {
        _layoutCollection = NewGraphGenerator.GenerateCollection(layouts);

        return this;
    }

    public LifCommandHandlerTestFixture WithNewerStoredLif(string vehicleTypeId)
    {
        _storedFiles.Add(NewGraphGenerator.GenerateLifFile(_uploadTimestamp.AddDays(1), [vehicleTypeId]));

        return this;
    }

    public LifCommandHandlerTestFixture WithFailingStore()
    {
        _lifRepository
            .Setup(r => r.AddFileAsync(It.IsAny<LifFile>(), It.IsAny<ICollection<Layout>>(), It.IsAny<ICollection<Layout>>()))
            .ThrowsAsync(new InvalidOperationException());

        return this;
    }
}