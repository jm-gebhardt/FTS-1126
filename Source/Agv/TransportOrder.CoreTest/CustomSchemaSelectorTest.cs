using Gebhardt.StoreWare.Agv.TransportOrder.Core;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest
{
    [TestClass]
    public class CustomSchemaSelectorTest
    {
        [TestMethod]
        public void RemoveDtoSuffixIfExists()
        {
            string typeNameWithoutSuffix = CustomSchemaSelector.RemoveDtoSuffix(typeof(AgvHub));
            typeNameWithoutSuffix.ShouldBe("AgvHub");

            string typeNameWithRemovedSuffix = CustomSchemaSelector.RemoveDtoSuffix(typeof(AgvDto));
            typeNameWithRemovedSuffix.ShouldBe("Agv");
        }
    }
}
