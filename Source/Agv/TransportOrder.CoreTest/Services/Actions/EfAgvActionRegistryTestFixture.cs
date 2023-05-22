using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions;

public class EfAgvActionRegistryTestFixture
    {
        private ServiceProvider Provider { get; }

        private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

        public EfActionGroupRegistry Registry { get; }

        public EfAgvActionRegistryTestFixture()
        {
            var collection = new ServiceCollection()
                .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); }
                );
            Provider = collection.BuildServiceProvider(true);
            ContextFactory = Provider.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>();
            Registry = new EfActionGroupRegistry(ContextFactory);
        }

        public Task InitTestData(ActionGroup actionGroup)
        {
            return Registry.InsertAsync(actionGroup);
        }

        public static List<AgvAction> CreateTestActionGroups()
        {
            var actionGroups = new List<AgvAction>
            {
                new("Action1".ToGuid(), string.Empty, ActionType.Pick, "NotStartedOrder1", "Action1".ToGuid(), "DST1") { State = ActionState.Created },
                new("Action2".ToGuid(), string.Empty, ActionType.Drop, "NotStartedOrder1", "Action1".ToGuid(), "DST1") { State = ActionState.Created },
                new("Action3".ToGuid(), string.Empty, ActionType.Pick, "NotStartedOrder2", "Action3".ToGuid(), "DST2") { State = ActionState.Created },
                new("Action4".ToGuid(), string.Empty, ActionType.Drop, "NotStartedOrder2", "Action3".ToGuid(), "DST2") { State = ActionState.Created },
                new("Action5".ToGuid(), "AGV011", ActionType.Pick, "UnfinishedOrder1", "Action5".ToGuid(), "DST3") { State = ActionState.Assigned },
                new("Action6".ToGuid(), "AGV011", ActionType.Drop, "UnfinishedOrder1", "Action5".ToGuid(), "DST3") { State = ActionState.Assigned },
                new("Action7".ToGuid(), "AGV012", ActionType.Pick, "UnfinishedOrder2", "Action7".ToGuid(), "DST4") { State = ActionState.Assigned },
                new("Action8".ToGuid(), "AGV012", ActionType.Drop, "UnfinishedOrder2", "Action7".ToGuid(), "DST4") { State = ActionState.Assigned },
                new("Action9".ToGuid(), "AGV001", ActionType.Pick, "UnfinishedOrder1", "Action9".ToGuid(), "DST3") { State = ActionState.Assigned },
                new("Action10".ToGuid(), "AGV001", ActionType.Drop, "UnfinishedOrder1", "Action9".ToGuid(), "DST3") { State = ActionState.Assigned },
                new("Action11".ToGuid(), "AGV002", ActionType.Pick, "UnfinishedOrder1", "Action9".ToGuid(), "DST3") { State = ActionState.Completed },
                new("Action12".ToGuid(), "AGV002", ActionType.Drop, "UnfinishedOrder1", "Action9".ToGuid(), "DST3") { State = ActionState.Completed }
            };
            return actionGroups;
        }

        public static List<AgvAction> CreateTestActions(int count = 4)
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
                actions.Add(new AgvAction(Guid.NewGuid(), agvId, actionType, orderId, orderId.ToGuid(), destination) { Nodes = nodes });
            }

            return actions;
        }
    }