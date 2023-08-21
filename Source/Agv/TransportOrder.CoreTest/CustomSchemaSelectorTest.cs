using Gebhardt.StoreWare.Agv.TransportOrder.Core;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest;

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