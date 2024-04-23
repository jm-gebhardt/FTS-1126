using Gebhardt.Acc.Common.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

/// <summary>
/// Testing fixture to test pollable service
/// </summary>
/// <typeparam name="TPollable"></typeparam>
[PublicAPI]
public abstract class PollableServiceTestFixture<TPollable> : MassTransitTestFixture where TPollable : class, IPollable
{
    /// <summary>
    /// Registered Pollable Service
    /// </summary>
    public TPollable Pollable { get; private set; } = null!;

    /// <summary>
    /// Test Fixture to be used for background service tests
    /// </summary>
    protected PollableServiceTestFixture()
    {
        AddServices(s => s.AddSingleton<TPollable>());
    }

    public new PollableServiceTestFixture<TPollable> Build()
    {
        base.Build();
        Pollable = Provider.GetRequiredService<TPollable>();
        return this;
    }
}