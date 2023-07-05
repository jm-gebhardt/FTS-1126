using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Messages
{
    /// <summary>
    /// Implementation of interface <see cref="IAgv"/> for testing.
    /// </summary>
    internal class Agv : IAgv
    {
        /// <inheritdoc/>
        [JetBrains.Annotations.UsedImplicitly]
        public Guid CorrelationId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        [JetBrains.Annotations.UsedImplicitly]
        public string AgvId { get; init; } = "";

        /// <inheritdoc/>
        [JetBrains.Annotations.UsedImplicitly]
        public string CurrentState { get; } = "";

        /// <inheritdoc/>'
        [JetBrains.Annotations.UsedImplicitly]
        public AgvStatus? AgvStatus { get; init; }

        /// <inheritdoc/>
        [JetBrains.Annotations.UsedImplicitly]
        public AgvGraph? AgvGraph { get; init; }

        /// <inheritdoc/>
        public bool IsOnline { get; } = true;

        /// <inheritdoc/>
        public bool IsLocked { get; set; } = false;

        /// <inheritdoc/>
        [JetBrains.Annotations.UsedImplicitly]
        public bool HasError { get; set; }

        /// <inheritdoc/>
        [JetBrains.Annotations.UsedImplicitly]
        public bool Assignable { get; } = true;

        /// <inheritdoc />
        public DateTime TimeOfStateTransition { get; set; }

        /// <inheritdoc />
        public DateTime TimeOfLockStateChanged { get; set; }
    }
}