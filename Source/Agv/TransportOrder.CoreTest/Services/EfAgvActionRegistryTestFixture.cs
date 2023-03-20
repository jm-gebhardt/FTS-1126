using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    /// <summary>
    /// Test fuxture for 
    /// </summary>
    public class EfAgvActionRegistryTestFixture
    {
        private ServiceProvider Provider { get; }
        private IDbContextFactory<SagaPersistenceDbContext> ContextFactory { get; }
        public EfAgvActionRegistry Registry { get; }

        public EfAgvActionRegistryTestFixture()
        {
            var collection = new ServiceCollection()
                .AddDbContextFactory<SagaPersistenceDbContext>(options =>
                {
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                }
            );
            Provider = collection.BuildServiceProvider(true);
            ContextFactory = Provider.GetService<IDbContextFactory<SagaPersistenceDbContext>>()!;
            Registry = new EfAgvActionRegistry(ContextFactory);
        }

        public void InitTestData(List<AgvAction> actions)
        {
            foreach (var action in actions)
            {
                Registry.Insert(action);
            }
        }

        public List<AgvAction> CreateTestActions(int count = 4)
        {
            var actions = new List<AgvAction>();
            string orderId = "";

            for (int i = 1; i <= count; i++)
            {
                if (i % 2 == 1) // Group 2 actions with one order/parent
                {
                    orderId = $"AGV{i / 2:D3}";
                }
                var actionType = i % 2 == 0 ? ActionType.Drop : ActionType.Pick; // Alternating pick/drop

                string agvId = $"AGV{i:D3}";
                string source = $"S{i:D3}";
                string destination = $"D{i:D3}";
                string[] nodes = { source, "W001", destination };
                actions.Add(new AgvAction(Guid.NewGuid(), agvId, actionType, orderId, orderId.ToGuid(), source, destination) { Nodes = nodes });
            }

            return actions;
        }
    }
}