using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;


namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
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


        /// <summary>
        /// Constructor.
        /// </summary>
        public AgvChooser()
        {
            _algorithms = new Dictionary<Guid, TGraphAlgorithm>();
        }


        /// <summary>
        /// Returns the graph algorithm addressed by correlation ID from <paramref name="agv"/>.
        /// When no such algorithm exists the factory tries to create such an algorithm.
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
                    new[] { typeof(AgvGraph) });
                if (constructor != null)
                {
                    if (agv == null || agv.AgvGraph == null)
                    {
                        throw new ArgumentException("AGV graph is missing.");
                    }
                    _algorithms[agv.CorrelationId] = (TGraphAlgorithm)constructor.Invoke(new object[] { agv.AgvGraph });
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
        /// <exception cref="ArgumentException">AGV enumeration is empty or missing or
        /// order information is missing or
        /// AGV graph within AGV data is missing.</exception>
        /// <exception cref="MissingMethodException">No fitting constructor for the graph algorithm found.</exception>
        /// <exception cref="System.Reflection.TargetInvocationException">The constructor of the algorithm has thrown an exception.</exception>
        [JetBrains.Annotations.PublicAPI]
        public Guid? ChooseAgv(IEnumerable<IAgv> agvDataEnumeration, Order order)
        {
            if (agvDataEnumeration == null)
            {
                throw new ArgumentException("AGV enumeration is missing.");
            }
            if (order == null)
            {
                throw new ArgumentException("Order information is missing.");
            }
            if (string.IsNullOrEmpty(order.Source))
            {
                throw new ArgumentException("Source is missing in order.");
            }
            if (string.IsNullOrEmpty(order.Destination))
            {
                throw new ArgumentException("Destination is missing in order.");
            }

            var routeListing = new Dictionary<Guid, IGraphAlgorithm.Route?>();

            foreach (var agv in agvDataEnumeration)
            {
                var algorithm = AlgorithmFactory(agv);
                IGraphAlgorithm.Route? route;
                if (agv.AgvStatus != null && !string.IsNullOrEmpty(agv.AgvStatus.LastNodeId))
                {
	                route = algorithm.CalculateRoute(agv.AgvStatus.LastNodeId, order.Source, order.Destination);
                }
                else
                {
	                route = algorithm.CalculateRoute(order.Source, order.Destination);
                }
                routeListing.Add(agv.CorrelationId, route);
            }

            if (routeListing.Count == 0)    // Checking here prevents trouble by double enumerating agvDataEnumeration.
            {
                throw new ArgumentException("AGV enumeration is empty.");
            }

            KeyValuePair<Guid, IGraphAlgorithm.Route?> first = routeListing.OrderByDescending(x => x.Value != null).ThenBy(x => x.Value).First();
            if (first.Value != null)
            {
                return first.Key;
            }
            return null;
        }
    }
}
