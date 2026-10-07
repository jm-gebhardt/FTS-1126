using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.RequestHandler;

public class FactsheetQueryHandlerTestFixture : MassTransitConsumerTestFixture<FactsheetQueryHandler>
{
    private const double Length = 1.5;
    private const double Width = 1.0;

    private readonly Mock<IAgvTypeRepository> _agvTypeRepository = new();

    public FactsheetQueryHandlerTestFixture WithAgvType(string rawData)
    {
        var agvType = new AgvType(FactsheetJsonGenerator.AgvTypeIdentifier, FactsheetJsonGenerator.Manufacturer, FactsheetJsonGenerator.SeriesName, Length, Width, rawData, true, false);
        _agvTypeRepository.Setup(x => x.GetAgvTypeAsync(FactsheetJsonGenerator.AgvTypeIdentifier)).ReturnsAsync(agvType);
        return this;
    }

    public new FactsheetQueryHandlerTestFixture Build()
    {
        AddServices(sc => sc.AddTransient(_ => _agvTypeRepository.Object));
        base.Build();
        return this;
    }

    public async Task<Response<GotFactsheet, FactsheetNotFound>> GetResponseAsync()
    {
        IRequestClient<GetFactsheet> client = TestHarness.Bus.CreateRequestClient<GetFactsheet>();
        return await client.GetResponse<GotFactsheet, FactsheetNotFound>(new GetFactsheet(Guid.NewGuid(), FactsheetJsonGenerator.AgvTypeIdentifier));
    }
}