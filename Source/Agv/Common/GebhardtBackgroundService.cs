using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Gebhardt.StoreWare.Agv.Common
{
    /// <summary>
    /// Manage service startup.
    /// </summary>
    /// <remarks>
    /// Runs service implementation inherited from <see cref="IWorker"/>.
    /// Responsible to handle initialization and failures at the moment when base infrastructure is not present (Gebhardt logger, ...).
    /// </remarks>
    public sealed class GebhardtBackgroundService : BackgroundService
    {
        private readonly IWorker _worker;
        private readonly ILogger<GebhardtBackgroundService> _logger;
        private readonly string _entryAssemblyName;

        /// <summary>
        /// Constructor called by default host builder.
        /// </summary>
        /// <param name="service">Service singleton (see <see cref="GebhardtStartup.BuildHost{T}"/>.</param>
        /// <param name="logger">Logger provided by default host builder.</param>
        public GebhardtBackgroundService(IWorker service, ILogger<GebhardtBackgroundService> logger)
        {
            _worker = service;
            _logger = logger;
            _entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? this.GetType().Name;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Signaling when cancellation is requested.</param>
        /// <returns>Task</returns>
        public override Task  StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{_entryAssemblyName} starting ...");
            try
            {
                _worker.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Called when the sService starts.
        /// </summary>
        /// <param name="stoppingToken">Signaling when cancellation is requested.</param>
        /// <remarks>
        /// The implementation should return a task that represents the lifetime of the long running operation(s) being performed.
        /// </remarks>
        /// <returns>A Task that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _worker.Run(stoppingToken);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Signaling when cancellation is requested.</param>
        /// <returns>Task</returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{_entryAssemblyName} stopping ...");
            try
            {
                _worker.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Disposing (cleanup unmanaged stuff).
        /// </summary>
        public override void Dispose()
        {
            _logger.LogInformation($"{_entryAssemblyName} disposing ...");
            base.Dispose();
        }
    }
}
