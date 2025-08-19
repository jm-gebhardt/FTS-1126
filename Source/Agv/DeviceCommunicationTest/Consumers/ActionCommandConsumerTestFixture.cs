using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using IAgvRepository = Gebhardt.Acc.DeviceCommunication.Common.Repositories.IAgvRepository;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public class ActionCommandConsumerTestFixture : ActionRepositoryConsumerTestFixture<ActionCommandConsumer>
{
    private readonly GraphRepositoryTestFixture _graphRepositoryTestFixture = new();

    public ActionCommandConsumerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient<IAgvRepository>(_ => Mock.Of<IAgvRepository>());
            s.AddTransient<IGraphRepository>(_ => _graphRepositoryTestFixture.GraphRepository);
        });
        Build();
    }

    public async Task FillGraphRepository()
    {
        await _graphRepositoryTestFixture.FillGraphRepository();
    }
}