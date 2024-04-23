using Gebhardt.Acc.Common.Testing.TestHarness;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Mediator;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using MediatorTestHarness = Gebhardt.Acc.Common.Testing.TestHarness.MediatorTestHarness;

namespace Gebhardt.Acc.Common.Testing.Fixtures;

/// <summary>
/// Testing fixture to test MassTransit Consumers and Request Client
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TConsumer"></typeparam>
[PublicAPI]
public class MediatorTestFixture<TRequest, TConsumer> : MassTransitConsumerTestFixture<TConsumer>
    where TRequest : class
    where TConsumer : class, IConsumer
{
    private readonly List<Action<IMediatorRegistrationConfigurator>> _configureMediatorAction = new();

    /// <summary>
    /// Started TestHarness
    /// </summary>
    public IMediatorTestHarness MediatorTestHarness { get; private set; } = null!;

    /// <summary>
    /// Mediator
    /// </summary>
    public IMediator? Mediator { get; private set; }

    public ITestHarness MassTransitTestHarness { get; private set; } = null!;

    /// <summary>
    /// Builds the service provider based on the service collection, in order to be able to register services lazy
    /// </summary>
    public new virtual MediatorTestFixture<TRequest, TConsumer> Build()
    {
        AddServices(s =>
        {
            s.AddMediator(configurator =>
            {
                AddRequestClient<TConsumer, TRequest>(configurator);
                foreach (Action<IMediatorRegistrationConfigurator> action in _configureMediatorAction)
                {
                    action.Invoke(configurator);
                }
            });
        });

        base.Build();

        Mediator = Provider.GetRequiredService<IMediator>();
        MassTransitTestHarness = Provider.GetTestHarness();
        MediatorTestHarness = new MediatorTestHarness(Mediator);
        MediatorTestHarness.Start();

        return this;
    }

    protected new async Task<MediatorTestFixture<TRequest, TConsumer>> BuildAsync()
    {
        await base.BuildAsync();
        return this;
    }

    private static void AddRequestClient<TConsumer2, TRequest2>(IMediatorRegistrationConfigurator cfg)
        where TRequest2 : class
        where TConsumer2 : class, IConsumer
    {
        cfg.AddConsumer<TConsumer2>();
        cfg.AddRequestClient<TRequest2>();
    }

    public virtual MediatorTestFixture<TRequest, TConsumer> ConfigureAdditionalRequestClient<TRequestHandler2, TRequest2>()
        where TRequest2 : class
        where TRequestHandler2 : class, IConsumer
    {
        _configureMediatorAction.Add(AddRequestClient<TRequestHandler2, TRequest2>);
        return this;
    }

    /// <summary>
    /// Gets the response from a request
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="request">The request object</param>
    /// <returns>The response object</returns>
    public async Task<Response<TResult>> GetResponseAsync<TResult>(TRequest request) where TResult : class
    {
        IRequestClient<TRequest> client = Mediator!.CreateRequestClient<TRequest>();
        Response<TResult> response = await client.GetResponse<TResult>(request);

        return response;
    }
}