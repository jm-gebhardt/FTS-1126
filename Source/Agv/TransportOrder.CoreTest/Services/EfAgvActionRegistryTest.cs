using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    [TestClass]
    public class EfAgvActionRegistryTest
    {
        #region Setup

        private static void CompareActions(AgvAction? agvAction1, AgvAction? agvAction2)
        {
            agvAction1.ShouldNotBeNull();
            agvAction2.ShouldNotBeNull();

            agvAction1.CorrelationId.ShouldBe(agvAction2.CorrelationId);
            agvAction1.ParentId.ShouldBe(agvAction2.ParentId);
            agvAction1.Sequence.ShouldBe(agvAction2.Sequence);
            agvAction1.Successor.ShouldBe(agvAction2.Successor);
            agvAction1.Predecessor.ShouldBe(agvAction2.Predecessor);
            agvAction1.AgvId.ShouldBe(agvAction2.AgvId);
            agvAction1.AgvCorrelationId.ShouldBe(agvAction2.AgvCorrelationId);
            agvAction1.State.ShouldBe(agvAction2.State);
            agvAction1.ActionType.ShouldBe(agvAction2.ActionType);
            agvAction1.OrderId.ShouldBe(agvAction2.OrderId);
            agvAction1.Source.ShouldBe(agvAction2.Source);
            agvAction1.Destination.ShouldBe(agvAction2.Destination);
            agvAction1.Nodes.ShouldBe(agvAction2.Nodes);
        }
        #endregion

        [TestMethod]
        public void Test_Insert_GetActionById()
        {
            var fixture = new EfAgvActionRegistryTestFixture();
            List<AgvAction> testActions = fixture.CreateTestActions();

            var testAction = testActions[0];

            var agvAction = fixture.Registry.GetActionById(testAction.CorrelationId);
            agvAction.ShouldBeNull(); // Should not exist yet

            fixture.Registry.Insert(testAction);

            agvAction = fixture.Registry.GetActionById(testAction.CorrelationId);
            CompareActions(agvAction, testAction);
        }

        [TestMethod]
        public void Test_GetPendingActions()
        {
            var fixture = new EfAgvActionRegistryTestFixture();
            List<AgvAction> testActions = fixture.CreateTestActions();
            fixture.InitTestData(testActions);

            List<AgvAction> pendingActions = fixture.Registry.GetPendingActions();

            for (int i = 0; i < pendingActions.Count; i++)
            {
                var testAction = testActions[i];
                var agvAction = pendingActions[i];
                CompareActions(agvAction, testAction);
            }
        }

        [TestMethod]
        public void Test_GetActionsByParentId()
        {
            var fixture = new EfAgvActionRegistryTestFixture();
            List<AgvAction> testActions = fixture.CreateTestActions();
            fixture.InitTestData(testActions);

            var testAction = testActions[0];

            List<AgvAction> groupedActions = fixture.Registry.GetActionsByParentId(testAction.ParentId);
            groupedActions.ShouldNotBeNull();
            groupedActions.Count.ShouldBe(2);

            for (int i = 0; i < groupedActions.Count; i++)
            {
                testAction = testActions[i];
                var agvAction = groupedActions[i];
                CompareActions(agvAction, testAction);
            }
        }

        [TestMethod]
        public void Test_UpdateState()
        {
            var fixture = new EfAgvActionRegistryTestFixture();
            List<AgvAction> testActions = fixture.CreateTestActions();
            fixture.InitTestData(testActions);

            // Negative test
            bool success = fixture.Registry.UpdateState(Guid.Empty, ActionState.Completed);
            success.ShouldBeFalse();

            // Positive test
            success = fixture.Registry.UpdateState(testActions[0].CorrelationId, ActionState.Completed);
            success.ShouldBeTrue();
            var agvAction = fixture.Registry.GetActionById(testActions[0].CorrelationId);
            agvAction.ShouldNotBeNull();
            agvAction.State.ShouldBe(ActionState.Completed);
        }

        [TestMethod]
        public void Test_GetSubsequentAction()
        {
            var fixture = new EfAgvActionRegistryTestFixture();
            List<AgvAction> testActions = fixture.CreateTestActions();
            testActions[0].State = ActionState.Completed;
            testActions[1].State = ActionState.Assigned;
            fixture.InitTestData(testActions);
            
            var subsequentAction = fixture.Registry.GetSubsequentAction(testActions[0].ParentId); // Search with parent id of first action
            CompareActions(subsequentAction, testActions[1]); // Should be second action
        }

        [TestMethod]
        public void Test_DeleteByParentId()
        {
            var fixture = new EfAgvActionRegistryTestFixture();
            List<AgvAction> testActions = fixture.CreateTestActions();
            fixture.InitTestData(testActions);

            var testAction = testActions[0];

            fixture.Registry.DeleteByParentId(testAction.ParentId);

            var deletedAction = fixture.Registry.GetActionById(testAction.CorrelationId);
            deletedAction.ShouldBeNull(); // Should not exist enymore
        }
    }
}
