using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;
using Gebhardt.Acc.TransportOrder.Messages.Requests.FilterTemplate;
using Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.FilterTemplate;

[TestClass]
public class FilterTemplateRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_AddFilterTemplate_ValidData_ReturnsCorrelationId()
    {
        var filterTemplate = new TransportOrder.Domain.Models.FilterTemplate.FilterTemplate() { Name = "name", FilterType = FilterType.Order };
        var id = Guid.NewGuid();
        var fixture = new FilterTemplateRequestHandlerTestFixture<AddFilterTemplate>()
            .WithFilterTemplate(filterTemplate, id);

        var response = await fixture.GetResponseAsync<AddedFilterTemplate>(new AddFilterTemplate("owner", filterTemplate, filterTemplate.Name));
        response.Message.FilterTemplateCorrelationId.ShouldBe(id);
    }

    [TestMethod]
    public async Task Consume_AddFilterTemplate_NameToLong_ThrowsResourceNameToLongException()
    {
        var filterTemplate = new TransportOrder.Domain.Models.FilterTemplate.FilterTemplate() { Name = "name", FilterType = FilterType.Order };
        var id = Guid.NewGuid();
        var fixture = new FilterTemplateRequestHandlerTestFixture<AddFilterTemplate>()
            .WithFilterTemplate(filterTemplate, id);

        await MediatorShould.ThrowRequestExceptionAsync<StringLengthExceededException>(async () => await fixture.GetResponseAsync<AddedFilterTemplate>(new AddFilterTemplate("owner", filterTemplate, new string('c', DbStringLimits.Description + 1))));
    }

    [TestMethod]
    public async Task Consume_UpdateFilterTemplate_ValidData_ReturnsTrue()
    {
        var filterTemplate = new TransportOrder.Domain.Models.FilterTemplate.FilterTemplate() { Name = "name", FilterType = FilterType.Order };
        var id = Guid.NewGuid();
        var fixture = new FilterTemplateRequestHandlerTestFixture<UpdateFilterTemplate>()
            .WithFilterTemplate(filterTemplate, id);

        var response = await fixture.GetResponseAsync<OwnedFilterModificationResult>(new UpdateFilterTemplate("owner", filterTemplate, id, filterTemplate.Name));
        response.Message.Result.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateFilterTemplate_NameToLong_ThrowsResourceNameToLongException()
    {
        var filterTemplate = new TransportOrder.Domain.Models.FilterTemplate.FilterTemplate() { Name = "name", FilterType = FilterType.Order };
        var id = Guid.NewGuid();
        var fixture = new FilterTemplateRequestHandlerTestFixture<UpdateFilterTemplate>()
            .WithFilterTemplate(filterTemplate, id);

        await MediatorShould.ThrowRequestExceptionAsync<StringLengthExceededException>(async () => await fixture.GetResponseAsync<OwnedFilterModificationResult>(new UpdateFilterTemplate("owner", filterTemplate, id, new string('c', DbStringLimits.Description + 1))));
    }

    [TestMethod]
    public async Task Consume_DeleteFilterTemplate_ValidData_ReturnsTrue()
    {
        var filterTemplate = new TransportOrder.Domain.Models.FilterTemplate.FilterTemplate() { Name = "name", FilterType = FilterType.Order };
        var id = Guid.NewGuid();
        var fixture = new FilterTemplateRequestHandlerTestFixture<DeleteFilterTemplate>()
            .WithFilterTemplate(filterTemplate, id);

        var response = await fixture.GetResponseAsync<OwnedFilterModificationResult>(new DeleteFilterTemplate(id, "owner"));
        response.Message.Result.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_GetFilterTemplates_ValidData_ReturnsTemplates()
    {
        var filterTemplate = new TransportOrder.Domain.Models.FilterTemplate.FilterTemplate() { Name = "name", FilterType = FilterType.Order };
        var id = Guid.NewGuid();
        var fixture = new FilterTemplateRequestHandlerTestFixture<GetFilterTemplates>()
            .WithFilterTemplate(filterTemplate, id);

        var response = await fixture.GetResponseAsync<GotFilterTemplates>(new GetFilterTemplates("owner"));
        response.Message.FilterTemplates.Count.ShouldBe(1);
    }
}