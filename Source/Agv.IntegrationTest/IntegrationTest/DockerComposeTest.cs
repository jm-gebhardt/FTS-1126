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
        private const string DockerRegistry = "registry.gitlab.com";
        private const string DockerRegistryRepository = "registry.gitlab.com/tar4/karis_virtual";

        private const string DockerRegistryUser = "gebhardt";

        private const string DockerRegistryPassword = "Crqsh3XfL-xcBTq6A8rw";

        /// <summary>
        /// Start and stop all services without VirtualAgv
        /// </summary>
        [TestMethod]
        public void ComposeTest()
        {
            var dc = new DockerCompose(EnvVars.ComposeFileEnvVar);
            dc.Up().ShouldBeTrue();
            dc.AreRunning().ShouldBeTrue();
            dc.Down().ShouldBeTrue();
            dc.AreRemoved().ShouldBeTrue();
        }

        /// <summary>
        /// Start and stop all services
        /// </summary>
        [TestMethod]
        public void CombinedComposeTest()
        {
            var dc = new DockerCompose(EnvVars.ComposeFileEnvVar);
            var dcVirtual = new DockerCompose(EnvVars.ComposeFileEnvVarVirtualAgv, DockerRegistry,
                DockerRegistryRepository, DockerRegistryUser, DockerRegistryPassword);
            dc.Up().ShouldBeTrue();
            dc.AreRunning().ShouldBeTrue();
            dcVirtual.Login();
            dcVirtual.Pull();
            dcVirtual.Up("karis0 mongo-provisioning").ShouldBeTrue();
            dcVirtual.AreRunning().ShouldBeTrue();
            dcVirtual.Down().ShouldBeTrue();
            dcVirtual.AreRemoved().ShouldBeTrue();
            dc.Down().ShouldBeTrue();
            dc.AreRemoved().ShouldBeTrue();
        }
    }
}