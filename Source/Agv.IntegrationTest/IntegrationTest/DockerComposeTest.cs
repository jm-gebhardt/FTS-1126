using Gebhardt.StoreWare.Agv.IntegrationTest.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

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
            dc.Up().ShouldBeTrue();
            dc.AreRunning().ShouldBeTrue();
            dc.Down().ShouldBeTrue();
            dc.AreRemoved().ShouldBeTrue();
        }
    }
}