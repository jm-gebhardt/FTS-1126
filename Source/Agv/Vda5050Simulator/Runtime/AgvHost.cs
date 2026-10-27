using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using System.Threading.Channels;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Processes commands and simulation ticks of one AGV sequentially via a mailbox.
/// Owns the <see cref="AgvModel"/> and decides when its state is published.
/// </summary>
public sealed class AgvHost
{
    private abstract record Message;
    
    private sealed record StartMessage : Message;
    
    private sealed record ShutdownMessage : Message;
    
    private sealed record CommandMessage(AgvCommand Command, TaskCompletionSource<CommandResult>? Reply = null) : Message;
    
    private sealed record TickMessage(TimeSpan Dt, TaskCompletionSource Done) : Message;

    private readonly Channel<Message> _mailbox = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions { SingleReader = true });
    private readonly AgvModel _model;
    private readonly ISimulationClock _clock;
    private readonly IAgvPublisher _publisher;
    private readonly TimeSpan _stateInterval;
    private readonly ILogger<AgvHost> _logger;
    private readonly IFactsheetProvider _factsheetProvider;
    private CancellationToken _cancellationToken;
    private Task? _loop;

    private uint _stateHeaderId;
    private uint _connectionHeaderId;
    private uint _factsheetHeaderId;
    private DateTimeOffset _lastStatePublish;

    public AgvHost(AgvModel model, ISimulationClock clock, IAgvPublisher publisher, TimeSpan stateInterval, IFactsheetProvider factsheetProvider, ILogger<AgvHost> logger)
    {
        _factsheetProvider = factsheetProvider;
        _model = model;
        _clock = clock;
        _publisher = publisher;
        _stateInterval = stateInterval;
        _logger = logger;
        _logger.LogInformation("{AgvId} - {Id}", model.AgvIdentity.SerialNumber, model.AgvIdentity.Id);
    }

    public AgvIdentity Id => _model.AgvIdentity;

    public void Start(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _loop = Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
        _mailbox.Writer.TryWrite(new StartMessage());
    }

    public void Post(AgvCommand command) => _mailbox.Writer.TryWrite(new CommandMessage(command));

    /// <summary>
    /// Sends a command and completes when the actor has handled it.
    /// </summary>
    public Task<CommandResult> SendAsync(AgvCommand command)
    {
        var reply = new TaskCompletionSource<CommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_mailbox.Writer.TryWrite(new CommandMessage(command, reply)))
        {
            reply.SetResult(CommandResult.Fail("Agv is stopped."));
        }
        return reply.Task;
    }

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
                            var commandResult = Handle(m.Command);
                            m.Reply?.TrySetResult(commandResult);
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
                    else if (message is CommandMessage { Reply: { } reply })
                    {
                        reply.TrySetException(ex);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private CommandResult Handle(AgvCommand command)
    {
        var result = CommandResult.Ok;
        switch (command)
        {
            case OrderReceived received:
                _logger.LogInformation("AGV {AgvId}: order {OrderId} update {UpdateId} received ({Nodes} nodes, {Edges} edges)",
                    Id, received.Order.OrderId, received.Order.OrderUpdateId, received.Order.Nodes.Count, received.Order.Edges.Count);
                var orderResult = _model.TryAcceptOrder(received.Order);
                if (orderResult.Decision == OrderDecision.Rejected)
                {
                    _logger.LogWarning("AGV {AgvId}: order {OrderId} update {UpdateId} rejected ({ErrorType}): {Description}",
                        Id, received.Order.OrderId, received.Order.OrderUpdateId, orderResult.Error?.ErrorType, orderResult.Error?.Description);
                }
                else
                {
                    _logger.LogInformation("AGV {AgvId}: order {OrderId} update {UpdateId}: {Decision}",
                        Id, received.Order.OrderId, received.Order.OrderUpdateId, orderResult.Decision);
                }
                break;

            case InstantActionsCommand received:
                HandleInstantActions(received);
                break;

            case FactsheetResolved resolved:
                _model.SetInstantActionState(resolved.ActionId, Vda5050Specifications.ActionTypes.FactsheetRequest, resolved.FactsheetJson == null ? ActionStatus.Failed : ActionStatus.Finished);
                if (resolved.FactsheetJson == null)
                {
                    _logger.LogWarning("AGV {AgvId}: no factsheet available for agv type '{AgvTypeIdentifier}'", Id, _model.AgvIdentity.AgvTypeIdentifier);
                }
                else
                {
                    PublishFactsheet(resolved.FactsheetJson);
                }
                break;

            case SwitchOperatingModeCommand switchMode:
                _logger.LogInformation("AGV {AgvId}: switching operating mode to {OperatingMode}", Id, switchMode.OperatingMode);
                _model.SwitchOperatingMode(switchMode.OperatingMode);
                break;

            case SetPositionCommand setPosition:
                _logger.LogInformation("AGV {AgvId}: setting position to ({X}, {Y})", Id, setPosition.X, setPosition.Y);
                result = _model.SetPositionManually(setPosition.X, setPosition.Y, setPosition.Theta, setPosition.MapId);
                break;

            case AddLoadCommand addLoad:
                _logger.LogInformation("AGV {AgvId}: adding load {LoadId}", Id, addLoad.LoadId);
                result = _model.AddLoad(addLoad.LoadId, addLoad.LoadType, addLoad.LoadPosition);
                break;

            case RemoveLoadCommand removeLoad:
                _logger.LogInformation("AGV {AgvId}: removing load {LoadId}", Id, removeLoad.LoadId);
                result = _model.RemoveLoad(removeLoad.LoadId);
                break;

            case MessageRejected rejected:
                _logger.LogWarning("AGV {AgvId}: {Kind} {Reference} rejected: {Errors}", Id, rejected.Topic, rejected.ReferenceId, string.Join("; ", rejected.Errors));
                
                _model.AddValidationError(rejected.Topic, rejected.ReferenceId, rejected.Errors);
                break;
        }
        PublishState();
        return result;
    }

    private void HandleInstantActions(InstantActionsCommand instantActionsCommand)
    {
        _logger.LogInformation("AGV {AgvId}: {Count} instantAction(s) received: {Types}", Id, instantActionsCommand.Actions.Count, string.Join(", ", instantActionsCommand.Actions.Select(a => a.ActionType)));

        foreach (var instantAction in instantActionsCommand.Actions)
        {
            if (instantAction.ActionType == Vda5050Specifications.ActionTypes.FactsheetRequest)
            {
                _model.SetInstantActionState(instantAction.ActionId, instantAction.ActionType, ActionStatus.Running);
                _ = ResolveFactsheetAsync(instantAction.ActionId);
                continue;
            }

            _model.AddValidationError(Vda5050Specifications.Subtopics.InstantActions, instantAction.ActionId, $"Cannot handle actionType {instantAction.ActionType}");

            _logger.LogWarning("AGV {AgvId}: Cannot handle instant action {actionId} of type {instantActionType}", Id, instantAction.ActionId, instantAction.ActionType);
        }
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

    private async Task ResolveFactsheetAsync(string actionId)
    {
        string? json = null;
        try
        {
            json = await _factsheetProvider.GetAsync(_model.AgvIdentity, _cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AGV {AgvId}: failed to fetch factsheet of agv type '{AgvTypeIdentifier}'", Id, _model.AgvIdentity.AgvTypeIdentifier);
        }
        
        Post(new FactsheetResolved(Id, actionId, json));
    }

    private void PublishFactsheet(string factsheetJson)
    {
        _factsheetHeaderId++;
        
        _publisher.PublishFactsheet(Id, _factsheetHeaderId, _clock.Now, factsheetJson);
    }

    private void PublishState()
    {
        _stateHeaderId++;
        _lastStatePublish = _clock.Now;
        _publisher.PublishState(Id, _model.CreateSnapshot(_stateHeaderId, _lastStatePublish));
    }
}
