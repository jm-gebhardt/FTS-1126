using Gebhardt.StoreWare.Agv.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest
{
    /// <summary>
    /// GebhardtStartup Test
    /// </summary>
    [TestClass]
    public class GebhardtStartupTest
    {
        /// <summary>
        /// Start TestWorker with no arguments provided
        /// This is a very lame integration test that does not wait for the actual services to get started
        /// </summary>
        [TestMethod]
        public void CreateHostBuilder_Build()
        {
            using var host = GebhardtStartup.CreateHostBuilder(Array.Empty<string>()).Build();
            host.ShouldNotBeNull();
        }
    }
}
