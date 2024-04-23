using Gebhardt.Acc.Common.WebApi;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi;

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