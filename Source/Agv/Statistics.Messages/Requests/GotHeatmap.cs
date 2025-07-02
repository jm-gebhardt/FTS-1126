using Gebhardt.Acc.Statistics.Domain.ReadModels;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Messages.Requests;

/// <summary>
/// Represents the response to the <see cref="GetHeatmap"/> request.
/// </summary>
/// <param name="Heatmap">Heatmap of scalar values.</param>
[PublicAPI]
public record GotHeatmap(ScalarHeatmap Heatmap);