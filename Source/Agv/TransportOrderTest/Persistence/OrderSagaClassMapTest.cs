using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Persistence
{
    [TestClass]
    public class OrderSagaClassMapTest
    {
        
        private class OrderSagaClassMapTestImpl : OrderSagaClassMap
        {
            /// <summary>
            /// Model Builder
            /// </summary>
            private readonly ModelBuilder _builder = new ();

            /// <summary>
            /// Call the protected configuration method in <see cref="OrderSagaClassMap" />
            /// </summary>
            public void CallConfiguration()
            {
                Configure(_builder.Entity<OrderSaga>(), _builder);
            }
        } 

        [TestMethod]
        public void CallConfigurationMethod_NoAssertPerformed()
        {
            var classMap = new OrderSagaClassMapTestImpl();
            classMap.CallConfiguration();
        }
    }
}