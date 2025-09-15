using Gebhardt.Acc.Common.Services;

namespace Gebhardt.Acc.PrismatCommunication.Simulation;

/// <summary>
/// Defines operations for controlling host order creation.
/// </summary>
public interface IHostOrderCreator : IPollable
{
    /// <summary>
    /// Pauses the generation of new host orders.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes the generation of new host orders.
    /// </summary>
    void Resume();
}