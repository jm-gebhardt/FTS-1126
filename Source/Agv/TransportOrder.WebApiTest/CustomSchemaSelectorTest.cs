using Gebhardt.StoreWare.Agv.Common.WebApi;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest;

[TestClass]
public class CustomSchemaSelectorTest
{
    [TestMethod]
    public void RemoveDtoSuffixIfExists()
    {
        string typeNameWithRemovedSuffix = CustomSchemaSelector.RemoveDtoSuffix(typeof(AgvDto));
        typeNameWithRemovedSuffix.ShouldBe("Agv");
    }
}