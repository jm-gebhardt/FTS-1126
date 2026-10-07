using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.CommandHandlers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

public class FactsheetCommandHandlerTestFixture : MassTransitConsumerTestFixture<FactsheetCommandHandler>
{
    private enum FactsheetResponse
    {
        Found,
        NotFound,
        Fault
    }

    private class FactsheetResponder(FactsheetResponse response) : IConsumer<GetFactsheet>
    {
        public async Task Consume(ConsumeContext<GetFactsheet> context)
        {
            (var correlationId, string agvTypeIdentifier) = context.Message;
            switch (response)
            {
                case FactsheetResponse.Found:
                    await context.RespondAsync(new GotFactsheet(correlationId, agvTypeIdentifier, FactsheetJsonGenerator.FactsheetJson));
                    break;

                case FactsheetResponse.NotFound:
                    await context.RespondAsync(new FactsheetNotFound(correlationId, agvTypeIdentifier));
                    break;

                default:
                    throw new InvalidOperationException("DeviceCommunication failure");
            }
        }
    }

    public FactsheetCache Cache { get; } = new();

    private FactsheetCommandHandlerTestFixture WithResponse(FactsheetResponse response)
    {
        AddServices(sc => sc.AddTransient(_ => new FactsheetResponder(response)));
        AddConsumers<FactsheetResponder>();
        return this;
    }

    public FactsheetCommandHandlerTestFixture WithUploadedFactsheet()
    {
        return WithResponse(FactsheetResponse.Found);
    }

    public FactsheetCommandHandlerTestFixture WithoutUploadedFactsheet()
    {
        return WithResponse(FactsheetResponse.NotFound);
    }

    public FactsheetCommandHandlerTestFixture WithFaultingDeviceCommunication()
    {
        return WithResponse(FactsheetResponse.Fault);
    }

    protected override void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        base.ConfigureMassTransitTestHarness(busRegistrationConfigurator);
        busRegistrationConfigurator.AddRequestClient<GetFactsheet>();
    }

    public new FactsheetCommandHandlerTestFixture Build()
    {
        AddServices(sc => sc.AddSingleton<IFactsheetCache>(Cache));
        base.Build();
        return this;
    }
}