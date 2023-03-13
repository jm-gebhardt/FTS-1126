using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Contracts
{
    /// <summary>
    /// Used to identify a message as correlated so that the CorrelationId can be returned
    /// </summary>
    /// <typeparam name="TKey">The type of the CorrelationId used</typeparam>
    public interface IParentedBy<out TKey>
    {
        /// <summary>
        /// Returns the Id of the parent element 
        /// </summary>
        [PublicAPI]
        public TKey ParentId { get; }

        /// <summary>
        /// The sequence or natural order, if any, in which the object has to integrate 
        /// </summary>
        [PublicAPI]
        public int Sequence { get; }

        /// <summary>
        /// The unique Id of the successive child element  
        /// </summary>
        [PublicAPI]
        public TKey? Successor { get; }

        /// <summary>
        /// The unique Id of the preceding child element  
        /// </summary>
        [PublicAPI]
        public TKey? Predecessor { get; }
    }
}
