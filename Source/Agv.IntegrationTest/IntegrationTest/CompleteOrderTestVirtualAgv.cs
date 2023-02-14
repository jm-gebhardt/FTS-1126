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

        private const string DockerRegistry = "registry.gitlab.com";
        private const string DockerRegistryRepository = "registry.gitlab.com/tar4/karis_virtual";

        private const string DockerRegistryUser = "gebhardt";

        private readonly string _dockerRegistryPassword =
            Environment.GetEnvironmentVariable(EnvVars.DockerRegistryPassword)
            ?? throw new InvalidOperationException($"Environment variable {EnvVars.DockerRegistryPassword} must be set.");

        /// <summary>
        /// Start all docker services before each test
        /// </summary>
        [TestInitialize]
        public void StartServices()
        {
            _dockerCompose.ShouldBeNull();
            _dockerComposeVirtualAgv.ShouldBeNull();

            _dockerCompose = new DockerCompose(EnvVars.ComposeFileEnvVar);
            _dockerComposeVirtualAgv = new DockerCompose(EnvVars.ComposeFileEnvVarVirtualAgv, DockerRegistry,
                DockerRegistryRepository, DockerRegistryUser, _dockerRegistryPassword);
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
        [TestCleanup]
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
            var restApi = new RestApi();
            var restAgv = await restApi.WaitForAgv("karis0");
            restAgv.ShouldNotBeNull();
            
            (await restApi.WaitForAgvData(restAgv["correlationId"]?.ToString())).ShouldNotBeNull();
            (await restApi.WaitForAgvGraph(restAgv["correlationId"]?.ToString())).ShouldNotBeNull();
            (await restApi.WaitForMap(restAgv["correlationId"]?.ToString())).ShouldNotBeNull();

            (await restApi.SetAgvIdle(restAgv["correlationId"]?.ToString())).ShouldBeTrue();

            var cancellationTokenSource = new CancellationTokenSource();
            await AddAndCompleteOrder("Order#1");

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        private async Task AddAndCompleteOrder(string orderId)
        {
            const string leNo = "123";
            const string source = "PVB02";
            const string destination = "PVB03";
            var context = new WcsDbContext();

            (await context.Database.CanConnectAsync()).ShouldBeTrue();

            await context.AddNewOrder(orderId, leNo, source, destination);
            await context.SaveChangesAsync();

            var restApi = new RestApi();
            var order = await restApi.WaitForOrder(orderId);
            order.ShouldNotBeNull();

            order = await restApi.WaitForOrder(orderId, "Final", true);
            order.ShouldNotBeNull();

            context.FromAgv.Any(o => o.OrderId == orderId && o.Status == OrderStatus.Finished).ShouldBeTrue();
        }
    }
}