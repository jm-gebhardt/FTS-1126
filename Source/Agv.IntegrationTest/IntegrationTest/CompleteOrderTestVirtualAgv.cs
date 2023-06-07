using Gebhardt.StoreWare.Agv.IntegrationTest.Utils;
using Gebhardt.StoreWare.Agv.IntegrationTest.Utils.Db;
using Gebhardt.StoreWare.Agv.IntegrationTest.Utils.Db.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.IntegrationTest
{
    /// <summary>
    /// CompleteOrderTest
    /// </summary>
    [TestClass]
    public class CompleteOrderTestVirtualAgv
    {
        private DockerCompose? _dockerCompose;
        private DockerCompose? _dockerComposeVirtualAgv;

        /// <summary>
        /// Start all docker services before each test
        /// </summary>
        //[TestInitialize]
        public void StartServices()
        {
            const string DockerRegistry = "registry.gitlab.com";
            const string DockerRegistryRepository = "registry.gitlab.com/tar4/karis_virtual";
            const string DockerRegistryUser = "gebhardt";
            string? dockerRegistryPassword = Environment.GetEnvironmentVariable(EnvVars.DockerRegistryPassword);
            if (string.IsNullOrEmpty(dockerRegistryPassword))
            {
                throw new InvalidOperationException($"Environment variable {EnvVars.DockerRegistryPassword} must be set.");
            }

            _dockerCompose.ShouldBeNull();
            _dockerComposeVirtualAgv.ShouldBeNull();

            _dockerCompose = new DockerCompose(EnvVars.ComposeFileEnvVar);
            _dockerComposeVirtualAgv = new DockerCompose(EnvVars.ComposeFileEnvVarVirtualAgv, DockerRegistry,
                DockerRegistryRepository, DockerRegistryUser, dockerRegistryPassword);
            _dockerCompose.AreRemoved().ShouldBeTrue();
            _dockerComposeVirtualAgv.AreRemoved().ShouldBeTrue();

            //Start Leitsteuerung
            _dockerCompose.Up().ShouldBeTrue();
            _dockerCompose.AreRunning().ShouldBeTrue();

            //Start Virtual AGV
            _dockerComposeVirtualAgv.Login().ShouldBeTrue();
            _dockerComposeVirtualAgv.Pull().ShouldBeTrue();
            _dockerComposeVirtualAgv.Up("karis0 mongo-provisioning").ShouldBeTrue();
            _dockerComposeVirtualAgv.AreRunning().ShouldBeTrue();
        }

        /// <summary>
        /// Remove all docker services after test
        /// </summary>
        //[TestCleanup]
        public void StopServices()
        {
            _dockerComposeVirtualAgv.ShouldNotBeNull();
            _dockerComposeVirtualAgv.Down().ShouldBeTrue();
            _dockerComposeVirtualAgv.AreRemoved().ShouldBeTrue();
            _dockerCompose.ShouldNotBeNull();
            _dockerCompose.Down().ShouldBeTrue();
            _dockerCompose.AreRemoved().ShouldBeTrue();
            _dockerCompose = null;
            _dockerComposeVirtualAgv = null;
        }

        /// <summary>
        /// Add one agv and process one order 
        /// </summary>
        [TestMethod]
        public async Task SingleVirtualAgvAdd_OrderProcessedSuccessful()
        {
            const string agvId = "karis0";
            const string orderId = "Order#1";
            const string leNo = "123";
            const string source = "PVB02";
            const string destination = "PVB03";

            var restApi = new RestApi("http://127.0.0.1:5000");

            var restAgv = await restApi.WaitForAgv(agvId);
            restAgv.ShouldNotBeNull();

            var agvCorrelationId = restAgv["correlationId"]?.ToString();
            agvCorrelationId.ShouldNotBeNullOrEmpty();

            (await restApi.WaitForAgvData(agvCorrelationId)).ShouldNotBeNull();
            (await restApi.WaitForAgvGraph(agvCorrelationId)).ShouldNotBeNull();
            (await restApi.WaitForMap(agvCorrelationId)).ShouldNotBeNull();

            (await restApi.UnlockAgv(agvCorrelationId)).ShouldBeTrue();

            var context = new WcsDbContext();

            (await context.Database.CanConnectAsync()).ShouldBeTrue();

            await context.AddNewOrder(orderId, leNo, source, destination);
            await context.SaveChangesAsync();

            var order = await restApi.WaitForOrder(orderId);
            order.ShouldNotBeNull();

            order = await restApi.WaitForOrder(orderId, "Completed", true);
            order.ShouldNotBeNull();

            context.FromAgv.Any(o => o.OrderId == orderId && o.Status == OrderStatus.Finished).ShouldBeTrue();
        }
    }
}