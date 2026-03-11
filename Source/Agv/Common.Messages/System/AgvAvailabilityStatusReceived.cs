namespace Gebhardt.Acc.Common.Messages.System;

/// <summary>
/// Provides a response to <see cref="RequestAgvAvailabilityStatus"/>.
/// </summary>
/// <param name="HasAvailableAgvs">True, if there are ready and available online agvs in the fleet manager.</param>
public record AgvAvailabilityStatusReceived(bool HasAvailableAgvs);