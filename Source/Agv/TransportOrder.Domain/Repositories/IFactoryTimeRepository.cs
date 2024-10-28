using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Abstracted interface for FactoryTimeRepository.
/// </summary>
public interface IFactoryTimeRepository
{
    /// <summary>
    /// Gets the factory time.
    /// </summary>
    Task<FactoryTime> GetFactoryTime();

    /// <summary>
    /// Sets the factory time.
    /// </summary>
    /// <param name="factoryTimeZone">The factory time zone.</param>
    Task SetFactoryTime(TimeZoneInfo factoryTimeZone);
}