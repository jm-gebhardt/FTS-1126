using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

internal class WifiMetricsEventHandlerTestFixture : GridPositionAttachedHandlerTestFixture<WifiMetricsEventHandler, AgvWifiMetricsReceived>
{
    private Mock<IMessageFilter<AgvWifiMetricsReceived>>? _filterMock;

    public WifiMetricsEventHandlerTestFixture FilterReturns(bool result)
    {
        _filterMock = new Mock<IMessageFilter<AgvWifiMetricsReceived>>();
        _filterMock.Setup(f => f.ShouldActAsync(It.IsAny<AgvWifiMetricsReceived>()))
            .ReturnsAsync(result);
        AddServices(services => { services.AddSingleton(_filterMock.Object); });
        return this;
    }

    public new WifiMetricsEventHandlerTestFixture Build()
    {
        base.Build();
        return this;
    }
}