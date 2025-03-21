using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Graphs.Requests;

/// <summary>
/// Represents the response to <see cref="GetTriggerMapping"/>.
/// </summary>
/// <param name="TriggerLocalizations">The localizations of the triggers.</param>
public record GotTriggerMapping(ICollection<TriggerLocalization> TriggerLocalizations);

/// <summary>
/// The trigger localization.
/// </summary>
/// <param name="TriggerName">The name of the trigger</param>
/// <param name="NodeId">The node id where there trigger is localized.</param>
/// <param name="AreaId">The area id where the trigger is localized</param>
[PublicAPI]
public record TriggerLocalization(string TriggerName, string? NodeId=null, Guid? AreaId = null );