using Gebhardt.StoreWare.Agv.Common;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.CommonTest
{
    /// <summary>
    /// GebhardtStartup Test
    /// </summary>
    [TestClass]
    public class GebhardtStartupTest
    {
        private class TestWorker : IWorker
        {
            public void Start() { }

            public async Task Run(CancellationToken stoppingToken)
            {
                await Task.Delay(0, stoppingToken);
            }

            public void Stop() { }
        }
        
        private class TestMassTransitWorker : IMassTransitWorker
        {
            public void Start() { }

            public async Task Run(CancellationToken stoppingToken)
            {
                await Task.Delay(0, stoppingToken);
            }

            public void Stop() { }
        }

        /// <summary>
        /// Start TestWorker with no arguments provided
        /// This is a very lame integration test that does not wait for the actual services to get started
        /// </summary>
        [TestMethod]
        public void BuildHost_IWorker_EmptyArgs_AllServicesRegistered()
        {
            using var host = GebhardtStartup.BuildHost<TestWorker>(Array.Empty<string>());
            Assert.IsInstanceOfType(host.Services.GetService<IWorker>(), typeof(TestWorker));
            IEnumerable<IHostedService> hostedServices = host.Services.GetServices<IHostedService>().ToArray();
            Assert.IsTrue(hostedServices.OfType<GebhardtBackgroundService>().Any());
            // MassTransit must not be registered
            Assert.IsFalse(hostedServices.OfType<MassTransitHostedService>().Any());
        }
        
        /// <summary>
        /// Start
        /// </summary>
        [TestMethod]
        public void BuildHost_TestMassTransitWorker_EmptyArgs_AllServicesRegistered()
        {
            using var host = GebhardtStartup.BuildHost<TestMassTransitWorker>(Array.Empty<string>());
            Assert.IsInstanceOfType(host.Services.GetService<IWorker>(), typeof(TestMassTransitWorker));
            IEnumerable<IHostedService> hostedServices = host.Services.GetServices<IHostedService>().ToArray();
            Assert.IsTrue(hostedServices.OfType<GebhardtBackgroundService>().Any());
            Assert.IsTrue(hostedServices.OfType<MassTransitHostedService>().Any());
        }

    }
}
