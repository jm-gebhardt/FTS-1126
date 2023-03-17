using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence
{
    /// <summary>
    /// Persistence DBContext used for sagas
    /// </summary>
    public class SagaPersistenceDbContext : SagaDbContext
    {
        /// <summary>
        /// Configuration section where the ConnectionString for AGV Master Control is located
        /// </summary>
        public const string ConfigSectionName = "AMC";

        /// <summary>
        /// Name of the migration table
        /// </summary>
        public const string MigrationTableName = $"__{nameof(SagaPersistenceDbContext)}Migration";

        /// <summary>
        /// Persistence DBContext used for sagas 
        /// </summary>
        /// <param name="options"></param>
        public SagaPersistenceDbContext(DbContextOptions<SagaPersistenceDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Class to Entity mappings
        /// This is called by <see cref="SagaDbContext.OnModelCreating"/>
        /// </summary>
        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get
            {
                yield return new OrderSagaClassMap();
                yield return new AgvSagaClassMap();
                yield return new MapClassMap();
                yield return new OrderChangedClassMap();
            }
        }

        /// <summary>
        /// A DbSet of <see cref="ConfigurationSetting"/> used for querying and saving instances.
        /// </summary>
        public DbSet<ConfigurationSetting> ConfigurationSettings { get; set; } = null!;

        /// <summary>
        /// DataSet "AgvAction"
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public DbSet<AgvAction> AgvActions { get; set; } = null!;

        /// <summary>
        /// A DbSet of <see cref="Node"/> used for querying and saving instances.
        /// </summary>
        public DbSet<Node> Nodes { get; set; } = null!;

        /// <summary>
        /// A DbSet of <see cref="Edge"/> used for querying and saving instances.
        /// </summary>
        public DbSet<Edge> Edges { get; set; } = null!;

        /// <summary>
        /// Configure the DbContext
        /// The default EF Logging is very chatty and prints each CommandExecution using LogLevel.Information.
        /// This configuration changes this to LogLevel.Debug
        /// Source: https://github.com/dotnet/efcore/issues/14523#issuecomment-491532384
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(c =>
            {
                // Logs from Microsoft.EntityFrameworkCore.Database.Command
                c.Log((RelationalEventId.CommandExecuted, LogLevel.Debug));
                // Logs from Microsoft.EntityFrameworkCore.Infrastructure
                c.Log((CoreEventId.ContextInitialized, LogLevel.Debug));
            });
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.BuildConfigurationSetting();

            EntityTypeBuilder<AgvAction> agvActions = modelBuilder.Entity<AgvAction>();
            agvActions.HasKey(nameof(AgvAction.CorrelationId));
            agvActions.Property(e => e.ParentId);
            agvActions.Property(e => e.Sequence);
            agvActions.Property(e => e.Successor);
            agvActions.Property(e => e.Predecessor);
            agvActions.Property(e => e.Nodes).HasJsonConversion();

            EntityTypeBuilder<Edge> edges = modelBuilder.Entity<Edge>();

            edges.HasKey(nameof(Edge.FromId), nameof(Edge.ToId));

            edges.HasOne(d => d.From)
                .WithMany(p => p.EdgesFrom)
                .HasForeignKey(d => d.FromId)
                .OnDelete(DeleteBehavior.ClientNoAction);

            edges.HasOne(d => d.To)
                .WithMany(p => p.EdgesTo)
                .HasForeignKey(d => d.ToId)
                .OnDelete(DeleteBehavior.ClientNoAction);
        }

        /// <inheritdoc/>
        public override int SaveChanges()
        {
            bool hasChanges = HasChanges();
            int result = base.SaveChanges();
            NotifyEntityChangeWatchers(hasChanges);

            return result;
        }

        /// <inheritdoc/>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            bool hasChanges = HasChanges();
            int result = await base.SaveChangesAsync(cancellationToken);
            NotifyEntityChangeWatchers(hasChanges);

            return result;
        }

        private bool HasChanges()
        {
            return ChangeTracker.Entries().Any(i => i.Entity is ConfigurationSetting && (i.State == EntityState.Modified || i.State == EntityState.Added));
        }

        private static void NotifyEntityChangeWatchers(bool hasChanges)
        {
            if (hasChanges)
            {
                EntityChangeObserver.Instance.OnChanged(EventArgs.Empty);
            }
        }
    }
}