using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Messages
{
    /// <summary>
    /// Implementation of interface <see cref="IAgv"/> for testing.
    /// </summary>
    internal class Agv : IAgv
    {
        /// <summary/>
        [JetBrains.Annotations.UsedImplicitly]
        public Guid CorrelationId { get; init; } = Guid.NewGuid();

        /// <summary/>
        [JetBrains.Annotations.UsedImplicitly]
        public string AgvId { get; init; } = "";

        /// <summary/>
        [JetBrains.Annotations.UsedImplicitly]
        public string CurrentState { get; } = "";

        /// <summary/>'
        [JetBrains.Annotations.UsedImplicitly]
        public AgvStatus? AgvStatus { get; init; }

        /// <summary/>
        [JetBrains.Annotations.UsedImplicitly]
        public AgvGraph? AgvGraph { get; init; }

        /// <summary/>
        public AgvData? AgvData { get; } = new(Guid.Empty,
            "",
            "",
            "",
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0,
            Array.Empty<AgvData.Sensor>());

        /// <summary/>
        public bool IsOnline { get; } = true;

        public bool IsLocked { get; set; } = false;

        /// <summary/>
        [JetBrains.Annotations.UsedImplicitly]
        public bool Assignable { get; } = true;
    }
}