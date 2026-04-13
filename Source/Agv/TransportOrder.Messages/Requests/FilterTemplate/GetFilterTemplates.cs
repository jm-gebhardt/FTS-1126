using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.FilterTemplate;

/// <summary>
/// Represents a request get all filter templates of a user.
/// </summary>
/// <param name="Owner">The owner of the filter.</param>
[PublicAPI]
public record GetFilterTemplates(string Owner);