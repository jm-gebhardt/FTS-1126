using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting the discovery of an Agv
/// </summary>
/// <param name="Agv"></param>
[PublicAPI]
public record AgvDiscovered(MessageContent.Agv Agv);