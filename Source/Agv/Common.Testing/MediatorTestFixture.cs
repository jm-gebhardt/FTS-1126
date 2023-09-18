using Gebhardt.StoreWare.Agv.Common.Testing.TestHarness;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.Common.Testing;

/// <summary>
/// Testing fixture to test MassTransit Consumers and Request Client
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TConsumer"></typeparam>
[PublicAPI]
public class MediatorTestFixture<TRequest, TConsumer>
    where TRequest : class
    where TConsumer : class, IConsumer
{
    private readonly List<Action<IServiceCollection>> _serviceActions = new();
    private readonly List<Action<IMediatorRegistrationConfigurator>> _configureMediatorAction = new();

    /// <summary>
    /// Service provider
    /// </summary>
    public ServiceProvider? Provider;

    /// <summary>
    /// Started TestHarness
    /// </summary>
    public IMediatorTestHarness? TestHarness;

    /// <summary>
    /// Mediator
    /// </summary>
    public IMediator? Mediator;

    /// <summary>
    /// Builds the service provider based on the service collection, in order to be able to register services lazy
    /// </summary>
    public virtual MediatorTestFixture<TRequest, TConsumer> Build()
    {
        var collection = new ServiceCollection()
            .AddMediator(configurator =>
            {
                AddRequestClient<TConsumer, TRequest>(configurator);
                foreach (Action<IMediatorRegistrationConfigurator> action in _configureMediatorAction)
                {
                    action.Invoke(configurator);
                }
            })
            .AddMassTransitTestHarness(x => { x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); }); });

        foreach (Action<IServiceCollection> service in _serviceActions)
        {
            service.Invoke(collection);
        }

        Provider = collection.BuildServiceProvider(true);
        Mediator = Provider.GetRequiredService<IMediator>();
        TestHarness = new MediatorTestHarness(Mediator);
        TestHarness.Start();

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
    /// Adds the service to the service collection
    /// </summary>
    /// <param name="services">The action of the service to add</param>
    /// <returns></returns>
    public MediatorTestFixture<TRequest, TConsumer> AddServices(Action<IServiceCollection> services)
    {
        _serviceActions.Add(services);
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