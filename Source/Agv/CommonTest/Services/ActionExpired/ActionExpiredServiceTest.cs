using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Services.ActionExpired;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services.ActionExpired
{
    [TestClass]
    public class ActionExpiredServiceTest
    {
        private AgvAction GetAgvAction(TimeSpan expirationDate)
        {
            var orderDetails = new OrderDetails(Guid.NewGuid(), "orderId", "SRC1", "DST1", "111", HostOrderCreator.Simulation, DateTime.UtcNow);
            return new AgvAction(Guid.NewGuid(), ActionType.Transport, orderDetails)
            {
                ExpirationDate = expirationDate
            };
        }


        [TestMethod]
        public void IsExpired_ReturnsTrue_WhenAgvActionIsExpired()
        {
            var agvAction = GetAgvAction(DateTime.UtcNow.AddHours(-1).TimeOfDay);

            IActionExpiredService actionExpiredService = new ActionExpiredService();
            bool isExpired = actionExpiredService.IsExpired(agvAction);

            isExpired.ShouldBeTrue();
        }

        [TestMethod]
        public void IsExpired_ReturnsFalse_WhenAgvActionIsNotExpired()
        {
            var agvAction = GetAgvAction(DateTime.UtcNow.AddHours(1).TimeOfDay);

            IActionExpiredService actionExpiredService = new ActionExpiredService();
            bool isExpired = actionExpiredService.IsExpired(agvAction);

            isExpired.ShouldBeFalse();
        }
    }
}
