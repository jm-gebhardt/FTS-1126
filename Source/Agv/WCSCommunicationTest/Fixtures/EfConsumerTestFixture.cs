using Gebhardt.StoreWare.Agv.Common.Testing;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures
{

    /// <summary>
    /// MassTransit consumer test fixture with EF DbContextFactory services added
    /// The factory automatically adds an in memory database
    /// </summary>
    public class EfConsumerTestFixture<T, TDbContext> : MassTransitTestFixture<T> where T : class, IConsumer where TDbContext : DbContext
    {

        /// <summary>
        /// DbContextFactory used to create TDbContext instances
        /// </summary>
        public readonly IDbContextFactory<TDbContext> DbContextFactory;

        /// <summary>
        /// MassTransit consumer test fixture with EF DbContextFactory services added
        /// </summary>
        /// <param name="services">Configure IServiceCollection</param>
        /// <param name="busRegistrationConfigurator">Configure IBusRegistrationConfigurator</param>
        public EfConsumerTestFixture(Action<IServiceCollection>? services = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null) :
            base(s =>
            {
                s.AddDbContextFactory<TDbContext>(options =>
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString())
                );
                services?.Invoke(s);
            }, busRegistrationConfigurator)
        {
            DbContextFactory = Provider.GetService<IDbContextFactory<TDbContext>>()!;
            EnsureDbCreated();
        }

        private void EnsureDbCreated()
        {
            using var context = DbContextFactory.CreateDbContext();
            context.Database.EnsureCreated();
        }
    }
}
