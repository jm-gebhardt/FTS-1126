using Gebhardt.Acc.Common.Notification.Models;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Notification;

[TestClass]
public class NotificationModelTest
{
    [TestMethod]
    public void NotificationToDto_ValidData_ConvertsToDto()
    {
        var notificationType = new NotificationType("Test");

        var notification = new Common.Notification.Models.Notification(notificationType, "TestTitle", "TestDesc", [], [new NotificationLink("TL1", "Ext"), new NotificationLink("TL2", NotificationLinkResource.Agv, Guid.NewGuid())]);

        var notificationDto = notification.ToDto();

        notificationDto.Title.ShouldBe(notification.Title);
        notificationDto.Description.ShouldBe(notification.Description);
        notificationDto.NotificationType.ShouldBe(notificationType.Name);
        notificationDto.Links.Count.ShouldBe(2);
        notificationDto.Links[0].Title.ShouldBe("TL1");
        notificationDto.Links[1].Title.ShouldBe("TL2");
    }
}