using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Graphs.Requests;

/// <summary>
/// Provides a request to get the location mapping of all triggers.
/// </summary>
[PublicAPI]
public record GetTriggerMapping;