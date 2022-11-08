using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence {

    /// <summary>
    /// Saga Persistence Extensions
    /// </summary>
    public static class SagaPersistenceConfigurationExtension
    {
        /// <summary>
        /// Extension to add Saga Persistence to given TSaga
        /// By default entity framework core persistence is used.
        /// It is expected that TDbContext is added to the services using AddDbContextFactory() 
        /// <see href="https://masstransit-project.com/usage/sagas/efcore.html" />
        /// </summary>
        /// <param name="configurator">ISagaRegistrationConfigurator</param>
        /// <param name="inMemory">If true, sagas are only store in memory (not for production)</param>
        /// <typeparam name="TSaga">Saga class</typeparam>
        /// <typeparam name="TDbContext">DbContext to be used</typeparam>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public static ISagaRegistrationConfigurator<TSaga> AddPersistence<TSaga, TDbContext>(
            this ISagaRegistrationConfigurator<TSaga> configurator,
            bool inMemory = false
            ) 
            where TDbContext: SagaDbContext 
            where TSaga : class, ISaga
        {
            if (inMemory)
            {
                configurator.InMemoryRepository();
            }
            else
            {
                configurator.EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                    r.ExistingDbContext<TDbContext>();
                });
            }
            return configurator;
        }
    }

}