using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.FilterTemplate;

public class FilterTemplateRequestHandlerTestFixture<TRequest> : MediatorTestFixture<TRequest, FilterTemplateRequestHandler, MockConsumer>
    where TRequest : class
{
    private readonly Mock<IOwnedFilterTemplateRepository> _filterTemplateRepository = new();

    public FilterTemplateRequestHandlerTestFixture()
    {
        AddServices(sc => sc.AddTransient(_ => _filterTemplateRepository.Object));
        base.Build();
    }

    public FilterTemplateRequestHandlerTestFixture<TRequest> WithFilterTemplate(TransportOrder.Domain.Models.FilterTemplate.FilterTemplate template, Guid correlationId)
    {
        _filterTemplateRepository.Setup(x => x.AddOwnedResource(template, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(correlationId);
        _filterTemplateRepository.Setup(x => x.UpdateOwnedResource(template, It.IsAny<string>(), correlationId, It.IsAny<string>())).ReturnsAsync(true);
        _filterTemplateRepository.Setup(x => x.DeleteResource(correlationId)).ReturnsAsync(true);
        _filterTemplateRepository.Setup(x => x.GetAllResources()).ReturnsAsync([new OwnedFilterTemplate(correlationId, "", template, template.Name)]);
        return this;
    }
}