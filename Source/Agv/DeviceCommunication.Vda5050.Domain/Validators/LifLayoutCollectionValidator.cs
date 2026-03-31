using FluentValidation;
using JetBrains.Annotations;
using Vdma.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Validators;

/// <summary>
/// Validates a <see cref="LifLayoutCollection"/>.
/// </summary>
[PublicAPI]
public class LifLayoutCollectionValidator : AbstractValidator<LifLayoutCollection>
{
    /// <inheritdoc cref="LifLayoutCollectionValidator"/>
    public LifLayoutCollectionValidator()
    {
        RuleFor(x => x.Layouts)
            .NotEmpty()
            .WithMessage("LIF layout collection must contain at least one layout.");

        RuleForEach(x => x.Layouts).ChildRules(layout =>
        {
            layout.RuleFor(l => l.Nodes)
                .NotEmpty()
                .WithMessage("Each layout must contain at least one node.");

            layout.RuleFor(l => l.Edges)
                .NotEmpty()
                .WithMessage("Each layout must contain at least one edge.");

            layout.RuleFor(l => l).Custom((l, context) =>
            {
                HashSet<string> nodeIds = l.Nodes.Select(n => n.NodeId).ToHashSet();

                foreach (var edge in l.Edges)
                {
                    if (!nodeIds.Contains(edge.StartNodeId))
                    {
                        context.AddFailure($"Edge '{edge.EdgeId}' references StartNodeId '{edge.StartNodeId}' which does not exist in layout '{l.LayoutId}'.");
                    }

                    if (!nodeIds.Contains(edge.EndNodeId))
                    {
                        context.AddFailure($"Edge '{edge.EdgeId}' references EndNodeId '{edge.EndNodeId}' which does not exist in layout '{l.LayoutId}'.");
                    }
                }
            });
        });
    }
}