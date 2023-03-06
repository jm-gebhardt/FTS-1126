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
        protected virtual void Build()
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

            ConfigureServices(collection);

            Provider = collection.BuildServiceProvider(true);
            Mediator = Provider.GetRequiredService<IMediator>();
            TestHarness = new MediatorTestHarness(Mediator);
            TestHarness.Start();
        }

        /// <summary>
        /// Is executed to configure the service collection
        /// </summary>
        /// <param name="collection"></param>
        protected virtual void ConfigureServices(IServiceCollection collection)
        {

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
