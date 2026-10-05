using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;

namespace Gebhardt.Acc.CommonTest.TelegramCommunication;

internal class SocketCommunicatorTestFixture
{
    private Mock<SocketBase> SocketMock { get; }

    public TestSender Sender { get; }

    public List<bool> StateChanges { get; } = [];

    public SocketCommunicatorTestFixture()
    {
        SocketMock = new Mock<SocketBase>(NullLoggerFactory.Instance.CreateLogger<SocketBase>()) { CallBase = false };
        SocketMock.Setup(s => s.TryInitialize()).Returns(true);
        SocketMock.Setup(s => s.TryInitializeAsync()).ReturnsAsync(true);

        var processor = new TestTelegramProcessor();
        var logger = NullLoggerFactory.Instance.CreateLogger<Sender<TestTelegram>>();

        Sender = new TestSender(SocketMock.Object, "test", processor, logger)
        {
            ConnectionStateChanged = isConnected => StateChanges.Add(isConnected)
        };
    }

    public async Task WaitForStateChangesAsync(int expectedCount, int timeoutMs = 5000)
    {
        int elapsed = 0;
        const int pollInterval = 20;
        while (StateChanges.Count < expectedCount && elapsed < timeoutMs)
        {
            await Task.Delay(pollInterval);
            elapsed += pollInterval;
        }
    }

    public class TestSender(
        SocketBase socket,
        string name,
        TelegramProcessorBase<TestTelegram> telegramProcessor,
        ILogger<Sender<TestTelegram>> logger)
        : Sender<TestTelegram>(socket, name, telegramProcessor, logger)
    {
        public void TriggerReconnect(string reason) => RequestSocketReconnect(reason);
    }

    public class TestTelegram : TelegramBase
    {
        /// <inheritdoc />
        public override bool Load(string telegram) => true;

        /// <inheritdoc />
        public override string CreateAcknowledgement() => string.Empty;
    }

    private class TestTelegramProcessor : TelegramProcessorBase<TestTelegram>
    {
        /// <inheritdoc />
        public override bool EnqueueTelegram(TestTelegram telegram) => false;

        /// <inheritdoc />
        public override bool GetNextTelegramToSend(out string telegram)
        {
            telegram = string.Empty;
            return false;
        }

        /// <inheritdoc />
        public override bool HasSendDemand() => false;

        /// <inheritdoc />
        public override Encoding GetEncoding() => Encoding.UTF8;

        /// <inheritdoc />
        public override bool ShouldReceiveTelegram() => false;

        /// <inheritdoc />
        public override void ProcessReceivedTelegram(string telegram)
        {
        }

        /// <inheritdoc />
        public override int GetTelegramLength() => 0;

        /// <inheritdoc />
        public override int GetWaitingTime() => 1000;

        /// <inheritdoc />
        public override void Cancel()
        {
        }

        /// <inheritdoc />
        public override void ResetCounter()
        {
        }

        /// <inheritdoc />
        public override bool GetUseLifeTelegrams() => false;
    }
}