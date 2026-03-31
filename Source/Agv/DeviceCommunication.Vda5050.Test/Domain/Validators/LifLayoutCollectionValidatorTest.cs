using FluentValidation.TestHelper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Validators;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Shouldly;
using Vdma.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Validators;

[TestClass]
public class LifLayoutCollectionValidatorTest
{
    private readonly LifLayoutCollectionValidator _validator = new();

    [TestMethod]
    public async Task Validate_EmptyLayouts_Fails()
    {
        var lif = new LifLayoutCollection { Layouts = [] };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.ShouldHaveValidationErrorFor(x => x.Layouts);
    }

    [TestMethod]
    public async Task Validate_LayoutWithNoNodes_Fails()
    {
        var layout = LifGenerator.GenerateLayout(edges: [LifGenerator.GenerateEdge("e1", "n1", "n2")]);
        var lif = new LifLayoutCollection { Layouts = [layout] };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Validate_LayoutWithNoEdges_Fails()
    {
        var layout = LifGenerator.GenerateLayout(nodes: [LifGenerator.GenerateNode("n1")]);
        var lif = new LifLayoutCollection { Layouts = [layout] };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Validate_EdgeReferencesMissingStartNode_Fails()
    {
        var node = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "missing", "n2");
        var layout = LifGenerator.GenerateLayout(nodes: [node], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout] };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("StartNodeId") && e.ErrorMessage.Contains("missing"));
    }

    [TestMethod]
    public async Task Validate_EdgeReferencesMissingEndNode_Fails()
    {
        var node = LifGenerator.GenerateNode("n1");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "missing");
        var layout = LifGenerator.GenerateLayout(nodes: [node], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout] };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("EndNodeId") && e.ErrorMessage.Contains("missing"));
    }

    [TestMethod]
    public async Task Validate_ValidCollection_Passes()
    {
        var n1 = LifGenerator.GenerateNode("n1");
        var n2 = LifGenerator.GenerateNode("n2");
        var edge = LifGenerator.GenerateEdge("e1", "n1", "n2");
        var layout = LifGenerator.GenerateLayout(nodes: [n1, n2], edges: [edge]);
        var lif = new LifLayoutCollection { Layouts = [layout] };

        TestValidationResult<LifLayoutCollection>? result = await _validator.TestValidateAsync(lif);

        result.ShouldNotHaveAnyValidationErrors();
    }
}