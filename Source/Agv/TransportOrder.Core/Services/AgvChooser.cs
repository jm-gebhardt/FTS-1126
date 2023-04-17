using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using IRoutingPlausibilityChecker = Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing.IRoutingPlausibilityChecker;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// API to get an appropriate AGV to handle a transport order.
    /// </summary>
    /// <typeparam name="TGraphAlgorithm">Algorithm to use for routing.
    /// This must be an implementation of <see cref="IGraphAlgorithm"/>.
    /// For creating the algorithm instances the public constructor is used taking <see cref="AgvGraph"/> as the only parameter.
    /// When no such constructor exists it tries to create the algorithm by using the public default constructor.</typeparam>
    [JetBrains.Annotations.PublicAPI]
    public class AgvChooser<TGraphAlgorithm> where TGraphAlgorithm : IGraphAlgorithm
    {
        private readonly Dictionary<Guid, TGraphAlgorithm> _algorithms;
        private readonly IRoutingPlausibilityChecker _routingPlausibilityChecker;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AgvChooser(IRoutingPlausibilityChecker routingPlausibilityChecker)
        {
            _routingPlausibilityChecker = routingPlausibilityChecker;
            _algorithms = new Dictionary<Guid, TGraphAlgorithm>();
        }


        /// <summary>
        /// Returns the graph algorithm addressed by correlation ID from <paramref name="agv"/>.
        /// When no such algorithm exists, the factory tries to create such an algorithm.
        /// </summary>
        /// <param name="agv"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">AGV graph is missing.</exception>
        /// <exception cref="MissingMethodException">No fitting constructor for the graph algorithm found.</exception>
        /// <exception cref="System.Reflection.TargetInvocationException">The constructor of the algorithm has thrown an exception.</exception>
        private TGraphAlgorithm AlgorithmFactory(IAgv agv)
        {
            if (!_algorithms.ContainsKey(agv.CorrelationId))
            {
                var constructor = typeof(TGraphAlgorithm).GetConstructor(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                    new[] { typeof(AgvGraph), typeof(IRoutingPlausibilityChecker) });
                if (constructor != null)
                {
                    if (agv == null || agv.AgvGraph == null)
                    {
                        throw new ArgumentException("AGV graph is missing.");
                    }
                    _algorithms[agv.CorrelationId] = (TGraphAlgorithm)constructor.Invoke(new object[] { agv.AgvGraph, _routingPlausibilityChecker });
                }
                else
                {
                    constructor = typeof(TGraphAlgorithm).GetConstructor(
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                        Type.EmptyTypes);
                    if (constructor != null)
                    {
                        _algorithms[agv.CorrelationId] = (TGraphAlgorithm)constructor.Invoke(null);
                    }
                    else
                    {
                        throw new MissingMethodException($"Constructor is missing talking {typeof(AgvGraph)} or default constructor missing.");
                    }
                }
            }

            return _algorithms[agv.CorrelationId];
        }


        /// <summary>
        /// Find AGV best to use for the requested order.
        /// </summary>
        /// <param name="agvDataEnumeration"></param>
        /// <param name="order"></param>
        /// <returns>Returns the correlation ID chosen or null when no fitting AGV could be found.</returns>
        /// <exception cref="NullReferenceException">Thrown if <see cref="OrderDetails.Source"/> or <see cref="OrderDetails.Destination"/> are null</exception>
        /// <exception cref="ArgumentException">Thrown if <see cref="OrderDetails.Source"/> or <see cref="OrderDetails.Destination"/> are empty</exception>
        /// <exception cref="MissingMethodException">No fitting constructor for the graph algorithm found.</exception>
        /// <exception cref="System.Reflection.TargetInvocationException">The constructor of the algorithm has thrown an exception.</exception>
        [JetBrains.Annotations.PublicAPI]
        public KeyValuePair<Guid, IGraphAlgorithm.Route>? ChooseAgv(IEnumerable<IAgv> agvDataEnumeration, OrderDetails order)
        {
            var routeListing = new Dictionary<Guid, IGraphAlgorithm.Route>();

            foreach (var agv in agvDataEnumeration)
            {
                var route = CalculateRoute(agv, order.Source, order.Destination);
                if (route != null)
                {
                    routeListing.Add(agv.CorrelationId, route);
                }
            }
            if (!routeListing.Any())
            {
                return null;
            }
            return routeListing.MinBy(x => x.Value);
        }

        /// <summary>
        /// Calculate Route for the given AGV and order
        /// </summary>
        /// <param name="agv"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <exception cref="ArgumentException">Thrown if <see cref="OrderDetails.Source"/> or <see cref="OrderDetails.Destination"/> are empty</exception>
        /// <returns>Route for the agv to process the order. Null if the route does not exist</returns>
        public IGraphAlgorithm.Route? CalculateRoute(IAgv agv, string source, string destination)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentException("Source is missing in order.");
            }
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException("Destination is missing in order.");
            }

            var algorithm = AlgorithmFactory(agv);
            if (agv.AgvStatus != null && !string.IsNullOrEmpty(agv.AgvStatus.LastNodeId))
            {
                return algorithm.CalculateRoute(agv.AgvStatus.LastNodeId, source, destination);
            }

            return algorithm.CalculateRoute(source, destination);
        }
    }
}
