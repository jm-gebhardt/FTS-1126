using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.TriggerDefinition;
using Gebhardt.Acc.TriggerService.Configuration;
using Gebhardt.Acc.TriggerService.Domain.Repositories;
using Gebhardt.Acc.TriggerService.Domain.Services;
using Gebhardt.Acc.TriggerService.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TriggerService.Test.Fixtures;

public class TriggerManagerTestFixture: FixtureBase
{
    private readonly Mock<ITriggerRepository> _triggerRepository = new();

    public readonly TriggerGenerator.Trigger TestTrigger = new();

    public readonly ITriggerManager TriggerManager;
    
    public TriggerManagerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => new Mock<ILogger<TriggerManager>>().Object);
            s.AddTransient(_ => new Mock<ILogger<ReflectionService>>().Object);
            s.AddTransient<ReflectionService>();
            s.AddSingleton(_ => new TriggerServiceConfiguration());
            s.AddSingleton(_ => _triggerRepository.Object);
            s.AddSingleton<ITriggerManager, TriggerManager>();
        });
        _triggerRepository.Setup(x => x.GetById(It.IsAny<Guid>())).ReturnsAsync(TestTrigger);
        _triggerRepository.Setup(x => x.UpdateTrigger(It.IsAny<Guid>(), It.IsAny<IFfcTrigger>())).Verifiable();
        Build();
        TriggerManager = Provider.GetRequiredService<ITriggerManager>();
    }

    public void VerifyTriggerUpdated()
    {
        _triggerRepository.Verify(x => x.UpdateTrigger(It.IsAny<Guid>(), It.IsAny<IFfcTrigger>()), Times.Once);
    }
}