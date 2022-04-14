using Gebhardt.StoreWare.Agv.DeviceCommunication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest
{

    /// <summary>
    /// Test for Worker
    /// </summary>
    [TestClass]
    public class WorkerTest
    {
        /// <summary>
        /// Test Worker can start
        /// </summary>
        [TestMethod]
        public void StartWorker_InputMocked()
        {
            Worker worker = new();
            worker.Start();
        }

        /// <summary>
        /// Test Worker can start
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task RunWorker_CancelAfter()
        {
            Worker worker = new();
            CancellationTokenSource cts = new();
            cts.CancelAfter(TimeSpan.FromSeconds(0.1));
            await worker.Run(cts.Token);
        }

        /// <summary>
        /// Test Worker can stop
        /// </summary>
        [TestMethod]
        public void StopWorker()
        {
            Worker worker = new();
            worker.Stop();
        }
    }
}
