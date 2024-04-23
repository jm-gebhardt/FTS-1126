using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the AgvSettingsReadModel
/// </summary>
/// <param name="AgvSettings">The agv settings.</param>
[UsedImplicitly]
public record GotAgvSettings(AgvSettings AgvSettings);