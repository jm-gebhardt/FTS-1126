using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Messages;

[TestClass]
public class LifSerialisationTest
{
    [TestMethod]
    public void LifValidationFailed_ValidData_SerialisesAllProperties()
    {
        var lifFile = new LifFile { Id = Guid.NewGuid(), MetaInformation = new LifFileMetaInformation(DateTime.UtcNow, "fileName", 299f, "user") { ValidationResults = [new LifValidationResult(ResourceType.File, null, Severity.Err, "description")] } };
        var message = new LifValidationFailed(lifFile);

        var fixture = new MessageSerialisationTestFixture();

        fixture.ValidateSerialization(message);
    }
}