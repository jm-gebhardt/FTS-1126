using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.Common.Services.ActionExpired
{
    /// <summary>
    /// Checks if the agv action is expired
    /// </summary>
    public interface IActionExpiredService
    {
        /// <summary>
        /// <inheritdoc cref="IActionExpiredService"/>
        /// </summary>
        /// <param name="action">Action to be check</param>
        /// <returns>True if the action is expired</returns>
        bool IsExpired(AgvAction action);
    }
}
