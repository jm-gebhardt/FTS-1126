using Gebhardt.StoreWare.Agv.Common.WebApi;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs.Filters;

public class FilterMock<T> : IHubFilter<T> where T : IMessageCreatedDate
{
    private readonly Mock<IHubFilter<T>> _filterMock;

    public FilterMock(bool shouldPublish)
    {
        _filterMock = new Mock<IHubFilter<T>>();
        _filterMock.Setup(f => f.ShouldPublish(It.IsAny<T>())).Returns(shouldPublish);
    }

    public bool ShouldPublish(T dto) => _filterMock.Object.ShouldPublish(dto);
}