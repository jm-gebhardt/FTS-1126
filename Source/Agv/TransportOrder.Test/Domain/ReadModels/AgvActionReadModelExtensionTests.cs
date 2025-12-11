using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Shouldly;
using Gebhardt.Acc.Common.Testing.Helpers;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.ReadModels;

[TestClass]
public class AgvActionReadModelExtensionTests
{
    [TestMethod]
    public void ConvertFromAgvActionWithFakeNodeType_ShouldReturnNull_WhenActionIsNull()
    {
        // Act
        var result = AgvActionReadModelExtension.ConvertFromAgvActionWithFakeNodeType(null);

        // Assert
        result.ShouldBeNull();
    }
        
    [TestMethod]
    public void ConvertFromAgvActionWithFakeNodeType_ShouldMapAllProperties()
    {
        // Arrange
        var action = AgvActionGenerator.GenerateAction();

        // Act
        var result = action.ConvertFromAgvActionWithFakeNodeType();

        // Assert
        result.ShouldNotBeNull();
        result.CorrelationId.ShouldBe(action.CorrelationId);
        result.ParentId.ShouldBe(action.ParentId);
        result.Sequence.ShouldBe(action.Sequence);
        result.State.ShouldBe(action.State);
        result.ActionType.ShouldBe(action.ActionType);
        result.TargetNodeId.ShouldBe(action.Target.Node?.Id);
        result.TargetNodeName.ShouldBe(action.Target.Node?.Name);
        result.Edges.ShouldBe(action.Edges.ToArray());
        result.CreatedDate.ShouldBe(action.CreatedDate);
        result.StartedDate.ShouldBe(action.StartedDate);
        result.CompletedDate.ShouldBe(action.CompletedDate);
        result.PausedDate.ShouldBe(action.PausedDate);
        result.PriorRunAndPauseDuration.ShouldBe(action.PriorRunAndPauseDuration);
        result.IsSkipped.ShouldBe(action.IsSkipped);
        result.InfoToAgv.ShouldBe(action.InfoToAgv);
        result.AdditionalInfo.ShouldBe(action.AdditionalInfo);
    }
}