using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Services.Filters;
using Moq;

namespace Gebhardt.Acc.Common.Testing.Filters;

public class FilterMock<T> : IMessageFilter<T> where T : IMessageCreatedDate
{
    private readonly Mock<IMessageFilter<T>> _filterMock;
    private bool _shouldPublish;

    public FilterMock()
    {
        _filterMock = new Mock<IMessageFilter<T>>();
        _filterMock.Setup(f => f.ShouldActAsync(It.IsAny<T>())).ReturnsAsync(ShouldPublish);
    }

    public FilterMock(bool shouldPublish) : this()
    {
        _shouldPublish = shouldPublish;
    }

    private bool ShouldPublish() => _shouldPublish;

    public FilterMock<T> Returns(bool shouldPublish)
    {
        _shouldPublish = shouldPublish;
        return this;
    }

    public Task<bool> ShouldActAsync(T value) => _filterMock.Object.ShouldActAsync(value);
}