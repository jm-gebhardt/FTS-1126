using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db
{
    /// <summary>
    /// Configurable DB Context
    /// </summary>
    public abstract class ConfigurableDbContext : DbContext
    {
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        protected ConfigurableDbContext(DbContextOptions options)
            : base(options)
        {
        }

        /// <summary>
        /// Entity Framework Mapping
        /// </summary>
        protected abstract IEnumerable<IClassMap> Configurations { get; }

        /// <summary>
        /// configure the model
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var configuration in Configurations)
            {
                configuration.Configure(modelBuilder);
            }
        }
        
    }
}