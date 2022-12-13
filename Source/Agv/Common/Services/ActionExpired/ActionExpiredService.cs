using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.Common.Services.ActionExpired
{
    /// <summary>
    /// <inheritdoc cref="IActionExpiredService"/>
    /// </summary>
    public class ActionExpiredService : IActionExpiredService
    {
        /// <inheritdoc />
        public bool IsExpired(AgvAction action)
        {
            var utcNow = DateTime.UtcNow;
            bool isExpired = utcNow.TimeOfDay > action.ExpirationDate;
            return isExpired;
        }
    }
}
