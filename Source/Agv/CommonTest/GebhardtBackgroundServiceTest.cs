using Gebhardt.StoreWare.Agv.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.CommonTest
{
    /// <summary>
    /// GebhardtBackgroundService Tests
    /// </summary>
    [TestClass]
    public class GebhardtBackgroundServiceTest
    {
        /// <summary>
        /// Test successful start
        /// </summary>
        [TestMethod]
        public async Task StartService_Successful_AllMocked()
        {
            var mockWorker = new Mock<IWorker>();
            var mockLogger = new Mock<ILogger<GebhardtBackgroundService>>();
            GebhardtBackgroundService backgroundService = new(mockWorker.Object, mockLogger.Object);

            await backgroundService.StartAsync(It.IsAny<CancellationToken>());

            Assert.AreEqual(TaskStatus.RanToCompletion, backgroundService.ExecuteTask.Status);
            mockWorker.Verify(w => w.Start(), Times.Once);
        }

        /// <summary>
        /// Test that start process will throw exception
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task StartService_WorkerThrowsExceptionOnStart()
        {
            var mockWorker = new Mock<IWorker>();
            mockWorker.Setup(w => w.Start()).Throws(new Exception());
            var mockLogger = new Mock<ILogger<GebhardtBackgroundService>>();
            GebhardtBackgroundService backgroundService = new(mockWorker.Object, mockLogger.Object);

            await backgroundService.StartAsync(It.IsAny<CancellationToken>());
            mockLogger.Verify(m => m.LogError(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test that stop process will throw exception
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task StartStopService_WorkerThrowsExceptionOnStop()
        {
            var mockWorker = new Mock<IWorker>();
            mockWorker.Setup(w => w.Stop()).Throws(new Exception());
            var mockLogger = new Mock<ILogger<GebhardtBackgroundService>>();
            GebhardtBackgroundService backgroundService = new(mockWorker.Object, mockLogger.Object);
            CancellationTokenSource cts = new();
            await backgroundService.StartAsync(cts.Token);
            await backgroundService.StopAsync(cts.Token);
            mockLogger.Verify(m => m.LogError(It.IsAny<string>()), Times.Once);
        }
    }
}
