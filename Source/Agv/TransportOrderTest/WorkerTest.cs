using Gebhardt.StoreWare.Agv.TransportOrder;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest
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

            mockedBus.Verify(b => b.Publish(It.IsAny<IMessageData>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Test Worker can stop
        /// </summary>
        [TestMethod]
        public void StopWorker()
        {
            var mockedBus = new Mock<IBus>();
            Worker worker = new(mockedBus.Object);
            worker.Stop();
        }

    }
}