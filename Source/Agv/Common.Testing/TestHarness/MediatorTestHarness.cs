using System.Diagnostics;
using JetBrains.Annotations;
using MassTransit.Mediator;
using MassTransit.Testing;
using MassTransit.Testing.Implementations;

namespace Gebhardt.StoreWare.Agv.Common.Testing.TestHarness;

/// <summary>
/// Provides a test harness for IMediator (adopted from
/// <a href="https://github.com/MassTransit/MassTransit/blob/develop/src/MassTransit/Testing/MediatorTestHarness.cs">
/// Mass
/// Transit
/// </a>
/// )
/// </summary>
[PublicAPI]
public class MediatorTestHarness : AsyncTestHarness, IMediatorTestHarness
{
    private BusTestConsumeObserver _consumed = null!;
    private BusTestPublishObserver _published = null!;
    private BusTestSendObserver _send = null!;

    /// <summary>
    /// Initializes a new <see cref="MediatorTestHarness" /> instance.
    /// </summary>
    /// <param name="mediator"></param>
    public MediatorTestHarness(IMediator mediator)
    {
        Mediator = mediator;
        TestTimeout = Debugger.IsAttached ? TimeSpan.FromMinutes(50) : TimeSpan.FromSeconds(10);
    }

    /// <inheritdoc />
    public IMediator Mediator { get; protected set; }

    /// <inheritdoc />
    public ISentMessageList Published => _send.Messages;

    /// <inheritdoc />
    public IReceivedMessageList Consumed => _consumed.Messages;

    /// <inheritdoc />
    public void Start()
    {
        _send = new BusTestSendObserver(TestTimeout, TimeSpan.FromSeconds(1), InactivityToken);
        _send.ConnectInactivityObserver(InactivityObserver);

        _published = new BusTestPublishObserver(TestTimeout, TimeSpan.FromSeconds(1), InactivityToken);
        _published.ConnectInactivityObserver(InactivityObserver);

        _consumed = new BusTestConsumeObserver(TestTimeout, InactivityToken);
        _consumed.ConnectInactivityObserver(InactivityObserver);

        Mediator.ConnectSendObserver(_send);
        Mediator.ConnectPublishObserver(_published);
        Mediator.ConnectConsumeObserver(_consumed);
    }
}