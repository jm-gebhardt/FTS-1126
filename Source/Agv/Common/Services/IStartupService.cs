namespace Gebhardt.Acc.Common.Services;

/// <summary>
/// Services that run once at application start before message and request processing beginns.
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// Triggered at application start.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);
}