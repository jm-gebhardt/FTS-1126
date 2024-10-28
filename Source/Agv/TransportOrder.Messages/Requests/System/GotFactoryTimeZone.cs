namespace Gebhardt.Acc.TransportOrder.Messages.Requests.System;

/// <summary>
/// Defines a result for <see cref="GetFactoryTimeZone"/>
/// </summary>
/// <param name="FactoryTime">The factoryTime.</param>
/// <param name="Dirty">If the factoryTime could be read.</param>
public record GotFactoryTimeZone(TimeZoneInfo FactoryTime, bool Dirty);