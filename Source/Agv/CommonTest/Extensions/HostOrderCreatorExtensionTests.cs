using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Extensions;

[TestClass]
public class HostOrderCreatorExtensionTests
{
    [TestMethod]
    public void ConvertToActionGroupCreator_FromCreatorDefinedInHostOrderCreated_ReturnsActionGroupCreator()
    {
        foreach (HostOrderCreator ordinal in Enum.GetValues(typeof(HostOrderCreator)))
        {
            ordinal.ConvertToActionGroupCreator().ShouldBe(Enum.Parse<ActionGroupCreator>(ordinal.ToString()));
        }
    }
}