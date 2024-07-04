using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TriggerService.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TriggerService.Test.Fixtures;

public class ReflectionServiceTestFixture: MassTransitTestFixture
{
    public readonly ReflectionService ReflectionService;

    public ReflectionServiceTestFixture()
    {
        AddServices(s =>
        {
            s.AddSingleton(_ => new Mock<ILogger<ReflectionService>>().Object);
            s.AddTransient<ReflectionService>();
        });

        Build();
        ReflectionService = Provider.GetRequiredService<ReflectionService>();
    }
}