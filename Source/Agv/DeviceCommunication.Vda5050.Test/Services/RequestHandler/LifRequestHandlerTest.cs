using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.RequestHandler;

[TestClass]
public class LifRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_DeleteLifFile_ValidData_RespondsSuccessAfterGraphSegmentDeletion()
    {
        var fixture = new LifRequestHandlerTestFixture();

        var response = await fixture.GetResponseAsync<Success>(new DeleteLifFile(fixture.LifFile.Id));

        response.Message.ShouldNotBeNull();
        fixture.VerifyLifDeleted();
    }
}