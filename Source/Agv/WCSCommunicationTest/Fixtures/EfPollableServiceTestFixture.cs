using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Testing;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures
{

    /// <summary>
    ///  MassTransit BackgroundService test fixture with EF DbContextFactory services added
    /// The factory automatically adds an in memory database with random name
    /// </summary>
    public class EfPollableServiceTestFixture<TPollable, TDbContext> : PollableServiceTestFixture<TPollable> 
        where TPollable : class, IPollable where TDbContext : DbContext
    {
        
        /// <summary>
        /// DbContextFactory used to create TDbContext instances
        /// </summary>
        public readonly IDbContextFactory<TDbContext> DbContextFactory;

        /// <summary>
        /// MassTransit BackgroundService test fixture with EF DbContextFactory services added
        /// </summary>
        /// <param name="services">Configure IServiceCollection</param>
        /// <param name="busRegistrationConfigurator">Configure IBusRegistrationConfigurator</param>
        public EfPollableServiceTestFixture(Action<IServiceCollection>? services = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null) :
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
