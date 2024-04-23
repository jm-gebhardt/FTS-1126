using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Extensions;

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