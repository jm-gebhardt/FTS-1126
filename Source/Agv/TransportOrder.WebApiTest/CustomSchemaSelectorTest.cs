using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest
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
