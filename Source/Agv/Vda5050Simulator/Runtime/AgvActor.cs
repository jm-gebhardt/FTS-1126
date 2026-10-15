using System.Threading.Channels;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Processes commands and simulation ticks of one AGV sequentially via a mailbox.
/// Owns the <see cref="AgvModel"/> and decides when its state is published.
/// </summary>
public sealed class AgvActor
{
    private abstract record Message;
    private sealed record StartMessage : Message;
    private sealed record ShutdownMessage : Message;
    private sealed record CommandMessage(AgvCommand Command) : Message;
    private sealed record TickMessage(TimeSpan Dt, TaskCompletionSource Done) : Message;

    private readonly Channel<Message> _mailbox = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions { SingleReader = true });
    private readonly AgvModel _model;
    private readonly ISimulationClock _clock;
    private readonly IAgvPublisher _publisher;
    private readonly TimeSpan _stateInterval;
    private readonly ILogger<AgvActor> _logger;
    private Task? _loop;

    private long _stateHeaderId;
    private long _connectionHeaderId;
    private DateTimeOffset _lastStatePublish;

    public AgvActor(AgvModel model, ISimulationClock clock, IAgvPublisher publisher, TimeSpan stateInterval, ILogger<AgvActor> logger)
    {
        _model = model;
        _clock = clock;
        _publisher = publisher;
        _stateInterval = stateInterval;
        _logger = logger;
    }

    public AgvId Id => _model.Id;

    public void Start(CancellationToken cancellationToken)
    {
        _loop = Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
        _mailbox.Writer.TryWrite(new StartMessage());
    }

    public void Post(AgvCommand command) => _mailbox.Writer.TryWrite(new CommandMessage(command));

    public Task AdvanceAsync(TimeSpan dt)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_mailbox.Writer.TryWrite(new TickMessage(dt, done)))
        {
            done.SetResult();
        }
        return done.Task;
    }

    public async Task StopAsync()
    {
        _mailbox.Writer.TryWrite(new ShutdownMessage());
        _mailbox.Writer.TryComplete();
        if (_loop != null)
        {
            await _loop;
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _mailbox.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    switch (message)
                    {
                        case StartMessage:
                            PublishConnection(ConnectionState.Online);
                            PublishState();
                            break;
                        case ShutdownMessage:
                            PublishConnection(ConnectionState.Offline);
                            break;
                        case CommandMessage m:
                            Handle(m.Command);
                            break;
                        case TickMessage t:
                            Advance(t.Dt);
                            t.Done.TrySetResult();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AGV {AgvId}: error while processing message", Id);
                    if (message is TickMessage tick)
                    {
                        tick.Done.TrySetException(ex);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Handle(AgvCommand command)
    {
        switch (command)
        {
            case OrderReceived received:
                _logger.LogInformation("AGV {AgvId}: order {OrderId} update {UpdateId} received ({Nodes} nodes, {Edges} edges)",
                    Id, received.Order.OrderId, received.Order.OrderUpdateId, received.Order.Nodes.Count, received.Order.Edges.Count);
                var result = _model.TryAcceptOrder(received.Order);
                if (result.Decision == OrderDecision.Rejected)
                {
                    _logger.LogWarning("AGV {AgvId}: order {OrderId} update {UpdateId} rejected ({ErrorType}): {Description}",
                        Id, received.Order.OrderId, received.Order.OrderUpdateId, result.Error?.ErrorType, result.Error?.Description);
                }
                else
                {
                    _logger.LogInformation("AGV {AgvId}: order {OrderId} update {UpdateId}: {Decision}",
                        Id, received.Order.OrderId, received.Order.OrderUpdateId, result.Decision);
                }
                break;

            case InstantActionsReceived received:
                _logger.LogInformation("AGV {AgvId}: {Count} instantAction(s) received: {Types}",
                    Id, received.Actions.Count, string.Join(", ", received.Actions.Select(a => a.Action.ActionType)));
                break;

            case MessageRejected rejected:
                _logger.LogWarning("AGV {AgvId}: {Kind} {Reference} rejected: {Errors}",
                    Id, rejected.MessageKind, rejected.ReferenceId, string.Join("; ", rejected.Errors));
                _model.AddValidationError(rejected.MessageKind, rejected.ReferenceId, rejected.Errors);
                break;
        }
        PublishState();
    }

    private void Advance(TimeSpan dt)
    {
        bool changed = _model.Advance(dt);
        if (changed || _clock.Now - _lastStatePublish >= _stateInterval)
        {
            PublishState();
        }
    }

    private void PublishConnection(ConnectionState state)
    {
        _connectionHeaderId++;
        _publisher.PublishConnection(Id, state, _connectionHeaderId, _clock.Now);
    }

    private void PublishState()
    {
        _stateHeaderId++;
        _lastStatePublish = _clock.Now;
        _publisher.PublishState(Id, _model.CreateSnapshot(_stateHeaderId, _lastStatePublish));
    }
}
