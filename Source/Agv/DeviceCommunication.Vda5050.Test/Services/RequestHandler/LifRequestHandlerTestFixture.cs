using FluentValidation;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.NewGraph.Requests;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using LifLayoutCollection = Vdma.Lif.LifLayoutCollection;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.RequestHandler;

public class LifRequestHandlerTestFixture : MediatorTestFixture<DeleteLifFile, LifRequestHandler, MockConsumer>
{
    private readonly Mock<ILifRepository> _lifRepository = new();
    private readonly Mock<IAgvRepository> _agvRepository = new();

    public LifFile LifFile { get; } = DomainLifGenerator.GenerateLifFile(DateTime.UtcNow, []);

    public LifRequestHandlerTestFixture()
    {
        _lifRepository.Setup(r => r.GetFileAsync(LifFile.Id)).ReturnsAsync(LifFile);
        _lifRepository.Setup(r => r.DeleteLifAsync(LifFile.Id)).Returns(Task.CompletedTask);
        _agvRepository.Setup(r => r.GetAllForVehicleTypesAsync(It.IsAny<ICollection<string>>())).ReturnsAsync(new List<Agv>());

        AddServices(s =>
        {
            s.AddTransient(_ => _lifRepository.Object);
            s.AddTransient(_ => _agvRepository.Object);
            s.AddTransient(_ => new Mock<IValidator<LifLayoutCollection>>().Object);
        });
        base.Build();
    }

    protected override void ConfigureMassTransitTestHarness(IBusRegistrationConfigurator busRegistrationConfigurator)
    {
        busRegistrationConfigurator.AddHandler<DeleteGraphSegment>(context => context.RespondAsync(new Success()));
        base.ConfigureMassTransitTestHarness(busRegistrationConfigurator);
    }

    public void VerifyLifDeleted()
    {
        _lifRepository.Verify(r => r.DeleteLifAsync(LifFile.Id), Times.Once);
    }
}