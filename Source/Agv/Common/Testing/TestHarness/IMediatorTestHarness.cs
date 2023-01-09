using JetBrains.Annotations;
using MassTransit.Mediator;
using MassTransit.Testing;

namespace Gebhardt.StoreWare.Agv.Common.Testing.TestHarness
{
    /// <summary>
    /// Provides an interface for mediator test harness (adopted from MassTransit's <see cref="IBaseTestHarness" />).
    /// </summary>
    [PublicAPI]
    public interface IMediatorTestHarness
    {
        /// <summary>
        /// Gets or sets the timeout for the test, used for any delay timers.
        /// </summary>
        TimeSpan TestTimeout { get; set; }

        /// <summary>
        /// Gets or sets the timeout specifying the elapsed time with no mediator activity after which the test could be completed.
        /// </summary>
        TimeSpan TestInactivityTimeout { get; set; }

        /// <summary>
        /// CancellationToken that is cancelled when the test inactivity timeout has elapsed with no mediator activity.
        /// </summary>
        CancellationToken InactivityToken { get; }

        /// <summary>
        /// Gets the task that is completed when the mediator inactivity timeout has elapsed with no mediator activity.
        /// </summary>
        Task InactivityTask { get; }

        /// <summary>
        /// Gets all messages that have been observed by the consume observer.
        /// </summary>
        IReceivedMessageList Consumed { get; }

        /// <summary>
        /// Gets all messages that have been observed by the send observer.
        /// Published messages are only observed by the SendObserver (not the PublishObserver),
        /// even though .Publish() has been called.
        /// </summary>
        ISentMessageList Published { get; }

        /// <summary>
        /// Gets the mediator.
        /// </summary>
        IMediator Mediator { get; }

        /// <summary>
        /// Sets the <see cref="CancellationToken" />, canceling the test execution
        /// </summary>
        void Cancel();

        /// <summary>
        /// Force the inactivity task to complete
        /// </summary>
        void ForceInactive();

        /// <summary>
        /// Starts the test harness by connecting observers to the mediator.
        /// </summary>
        void Start();
    }
}
