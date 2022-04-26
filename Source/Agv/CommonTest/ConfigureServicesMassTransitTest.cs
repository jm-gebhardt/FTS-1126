using Gebhardt.StoreWare.Agv.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.CommonTest
{
    /// <summary>
    /// Tests for ConfigureServicesMassTransit
    /// </summary>
    [TestClass]
    public class ConfigureServicesMassTransitTest
    {

        /// <summary>
        /// Test no RabbitMQ config provided
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConfigureServices_NoConfigProvided()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string>()
                ).Build();
            ServiceCollection services = new ();
            ConfigureServicesMassTransit.ConfigureServices(services, configuration);
        }

        /// <summary>
        /// Test valid RabbitMQ config provided
        /// </summary>
        [TestMethod]
        public void ConfigureServices_ValidConfigProvided()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string> {{ConfigureServicesMassTransit.ConfigSectionName, ""}}
                ).Build();
            ServiceCollection services = new ();

            ConfigureServicesMassTransit.ConfigureServices(services, configuration);
        }
    }
}