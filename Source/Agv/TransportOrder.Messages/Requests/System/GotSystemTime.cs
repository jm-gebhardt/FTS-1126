namespace Gebhardt.Acc.TransportOrder.Messages.Requests.System;

/// <summary>
/// Defines a result for the <see cref="GetSystemTime"/>
/// </summary>
/// <param name="SystemTime"></param>
public record GotSystemTime(DateTime SystemTime);