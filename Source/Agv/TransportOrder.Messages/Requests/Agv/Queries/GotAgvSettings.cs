using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the AgvSettings
/// </summary>
/// <param name="AgvSettings">The agv settings.</param>
[UsedImplicitly]
public record GotAgvSettings(AgvSettings AgvSettings);