using Gebhardt.StoreWare.Agv.Common.Testing.TestHarness;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.DependencyInjection;
using MediatorTestHarness = Gebhardt.StoreWare.Agv.Common.Testing.TestHarness.MediatorTestHarness;

namespace Gebhardt.StoreWare.Agv.Common.Testing
{
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
                .AddMediator(cfg =>
                {
                    cfg.AddConsumer<TConsumer>();
                    cfg.AddRequestClient<TRequest>();
                })
                .AddMassTransitTestHarness(x =>
                {
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                });

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
        public async Task<Response<TResult>> GetResponseAsync<TResult>(TRequest request) where TResult: class
        {
            IRequestClient<TRequest> client = Mediator!.CreateRequestClient<TRequest>();
            Response<TResult> response = await client.GetResponse<TResult>(request);

            return response;
        }
    }

}
