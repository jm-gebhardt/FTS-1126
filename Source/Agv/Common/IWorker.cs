namespace Gebhardt.StoreWare.Agv.Common
{
    /// <summary>
    /// Interface for all background services.
    /// </summary>
    public interface IWorker
    {
        /// <summary>
        /// Service initialization can be done here.
        /// </summary>
        public void Start();

        /// <summary>
        /// Should implement the service main loop.
        /// </summary>
        /// <param name="stoppingToken">Signaling when cancellation is requested.</param>
        /// <returns>A Task that represents the long running operations.</returns>
        public Task Run(CancellationToken stoppingToken);

        /// <summary>
        /// Service deinitialization can be done here.
        /// </summary>
        public void Stop();
    }

}
