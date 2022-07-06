using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Persistence
{
    /// <summary>
    /// AgvSagaClassMapTest
    /// </summary>
    [TestClass]
    public class AgvSagaClassMapTest
    {
        private class AgvSagaClassMapTestImpl : AgvSagaClassMap
        {
            /// <summary>
            /// Model Builder
            /// </summary>
            private readonly ModelBuilder _builder = new ();

            /// <summary>
            /// Call the protected configuration method in <see cref="AgvSagaClassMap" />
            /// </summary>
            public void CallConfiguration()
            {
                Configure(_builder.Entity<AgvSaga>(), _builder);
            }
        } 

        /// <summary>
        /// This is a simple that that calls the configuration method
        /// </summary>
        [TestMethod]
        public void CallConfigurationMethod_NoAssertPerformed()
        {
            var classMap = new AgvSagaClassMapTestImpl();
            classMap.CallConfiguration();
        }
    }
}