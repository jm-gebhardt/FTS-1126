using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.WCSCommunication.CmdOptions;
using Gebhardt.Acc.WCSCommunication.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.WCSCommunication.WcsApi.SimulationMode;

/// <summary>
/// Simulates the creation of host orders
/// </summary>
public class SimulationHostOrderService : IPollable
{
    /// <summary>
    /// IPollable interval
    /// </summary>
    public int PollingInterval { get; }

    private readonly string _runtimeIdentifierOrder = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
    private readonly ILogger<SimulationHostOrderService> _logger;
    private readonly IBus _bus;
    private readonly string[] _simSources;
    private readonly string[] _simDestinations;
    private readonly bool _modeSequential;
    private readonly Random _random = new();
    private long _currentSourceIndex;
    private long _currentDestinationIndex;
    private uint _orderIdCounter;

    /// <summary>
    /// Create simulator for creating host order
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="bus">MassTransit message bus</param>
    /// <param name="simulationOptions">Commandline options</param>
    public SimulationHostOrderService(ILogger<SimulationHostOrderService> logger, IBus bus, SimulateOptions simulationOptions)
    {
        _bus = bus;
        _logger = logger;

        _simSources = simulationOptions.Sources != null ? simulationOptions.Sources.ToArray() : Array.Empty<string>();
        _simDestinations = simulationOptions.Destinations != null ? simulationOptions.Destinations.ToArray() : Array.Empty<string>();
        _modeSequential = simulationOptions.ModeSequential;
        PollingInterval = simulationOptions.Interval; // simulation options is specified in milliseconds
    }

    /// <summary>
    /// Run simulation cycle
    /// </summary>
    public async Task Poll(CancellationToken cancellationToken = default)
    {
        await SimulateHostOrders(cancellationToken);
    }

    /// <summary>
    /// Creates dummy HostOrders for simulation
    /// </summary>
    private async Task SimulateHostOrders(CancellationToken cancellationToken)
    {
        string orderId = NextOrderId();

        string src = _modeSequential ? _simSources[_currentSourceIndex] : _simSources[_random.Next(_simSources.Length)];
        string dst = _modeSequential ? _simDestinations[_currentDestinationIndex] : _simDestinations[_random.Next(_simDestinations.Length)];
        if (_modeSequential)
        {
            _currentSourceIndex = (_currentSourceIndex + 1) % _simSources.Length;
            _currentDestinationIndex = (_currentDestinationIndex + 1) % _simDestinations.Length;
        }

        const string leNo = "9999";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, src, dst, leNo, HostOrderCreator.Simulation);

        await _bus.Publish(new CreateHostOrder(orderDetails), cancellationToken);

        _logger.LogInformation("Simulation HostOrder created: CorrelationId = {CorrelationId}, OrderId = {OrderId}, Source = {Src}, Dest = {Dst}, LeNo = {LeNo}", correlationId, orderId, src, dst, leNo);
    }

    private string NextOrderId()
    {
        return $"SIM_{_runtimeIdentifierOrder}_{_orderIdCounter++:D8}";
    }
}