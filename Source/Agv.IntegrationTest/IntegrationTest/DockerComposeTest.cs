using Gebhardt.StoreWare.Agv.IntegrationTest.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.IntegrationTest
{
    /// <summary>
    /// Starts and stops docker compose service
    /// </summary>
    [TestClass]
    public class DockerComposeTest
    {
        /// <summary>
        /// Start and stop all services
        /// </summary>
        [TestMethod]
        public void ComposeTest()
        {
            var dc = new DockerCompose();
            Assert.IsTrue(dc.Up());
            Assert.IsTrue(dc.AreRunning());
            Assert.IsTrue(dc.Down());
            Assert.IsTrue(dc.AreRemoved());
        }
    }
}