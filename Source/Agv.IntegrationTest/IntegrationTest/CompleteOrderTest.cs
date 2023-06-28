using Gebhardt.StoreWare.Agv.IntegrationTest.Agv;
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
    public class CompleteOrderTest
    {
        private DockerCompose? _dockerCompose;

        /// <summary>
        /// Start all docker services before each test
        /// </summary>
        [TestInitialize]
        public void StartServices()
        {
            _dockerCompose.ShouldBeNull();
            _dockerCompose = new DockerCompose(EnvVars.ComposeFileEnvVar);
            _dockerCompose.AreRemoved().ShouldBeTrue();
            _dockerCompose.Up().ShouldBeTrue();
            _dockerCompose.AreRunning().ShouldBeTrue();
        }

        /// <summary>
        /// Remove all docker services after test
        /// </summary>
        [TestCleanup]
        public void StopServices()
        {
            _dockerCompose.ShouldNotBeNull();
            _dockerCompose.Down().ShouldBeTrue();
            _dockerCompose.AreRemoved().ShouldBeTrue();
            _dockerCompose = null;
        }

        /// <summary>
        /// Add one agv and process one order 
        /// </summary>
        [TestMethod]
        public async Task SingleAgvAdd_OrderProcessedSuccessful()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var agv = await AddAgv("Agv#1", cancellationTokenSource.Token);
            var _ = agv.SubscribeToCompleteOrdersSuccessfully();
            await AddAndCompleteOrder("Order#1");

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        /// <summary>
        /// Add two agvs and process four orders
        /// </summary>
        [TestMethod]
        public async Task TwoAgvAdd_FourOrderProcessedSuccessful()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var agv0 = await AddAgv("Agv#1", cancellationTokenSource.Token);
            var agv1 = await AddAgv("Agv#2", cancellationTokenSource.Token);
            _ = agv0.SubscribeToCompleteOrdersSuccessfully();
            _ = agv1.SubscribeToCompleteOrdersSuccessfully();
            await AddAndCompleteOrder("Order#1");
            await AddAndCompleteOrder("Order#2");
            await AddAndCompleteOrder("Order#3");
            await AddAndCompleteOrder("Order#4");

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        private async Task<TestAgv> AddAgv(string agvId, CancellationToken token)
        {
            var restApi = new RestApi();

            var agv = new TestAgv(agvId);
            await agv.SubscribeDataTopics();

            _ = agv.StartSendingStatus(token);

            var restAgv = await restApi.WaitForAgv(agv.AgvId);
            restAgv.ShouldNotBeNull();

            string? correlationId = restAgv["correlationId"]?.ToString();
            correlationId.ShouldNotBeNull();

            (await restApi.WaitForAgvData(correlationId)).ShouldNotBeNull();
            (await restApi.WaitForAgvGraph(correlationId)).ShouldNotBeNull();
            (await restApi.WaitForMap(correlationId)).ShouldNotBeNull();
            (await restApi.UnlockAgv(correlationId)).ShouldBeTrue();

            return agv;
        }

        private async Task AddAndCompleteOrder(string orderId)
        {
            const string leNo = "123";
            const string source = "SRC_01";
            const string destination = "DST_01";
            var context = new WcsDbContext();

            (await context.Database.CanConnectAsync()).ShouldBeTrue();

            await context.AddNewOrder(orderId, leNo, source, destination);
            await context.SaveChangesAsync();

            var restApi = new RestApi();
            var order = await restApi.WaitForOrder(orderId);
            order.ShouldNotBeNull();

            order = await restApi.WaitForOrder(orderId, "Completed", true);
            order.ShouldNotBeNull();

            context.FromAgv.Any(o => o.OrderId == orderId && o.Status == OrderStatus.Finished).ShouldBeTrue();
        }
    }
}