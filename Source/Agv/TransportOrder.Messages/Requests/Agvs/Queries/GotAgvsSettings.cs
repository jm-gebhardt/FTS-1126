using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the <see cref="GetAgvsSettings"/> request.
/// </summary>
public record GotAgvsSettings(List<AgvSettings> AgvSettings);