using Gebhardt.StoreWare.Agv.WCSCommunication;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest
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
        public void StartWorker()
        {
            var mockedBus = new Mock<IBus>();
            Worker worker = new(mockedBus.Object);
            worker.Start();
        }

        /// <summary>
        /// Test Worker can start
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task RunWorker_CancelAfter()
        {
            var mockedBus = new Mock<IBus>();
            Worker worker = new(mockedBus.Object);
            CancellationTokenSource cts = new();
            cts.CancelAfter(TimeSpan.FromSeconds(0.1));
            await worker.Run(cts.Token);

        }

        /// <summary>
        /// Test Worker can stop
        /// </summary>
        [TestMethod]
        public void StopWorker_PossibleWithoutPreviousStart()
        {
            var mockedBus = new Mock<IBus>();
            Worker worker = new(mockedBus.Object);
            worker.Stop();
        }
    }
}