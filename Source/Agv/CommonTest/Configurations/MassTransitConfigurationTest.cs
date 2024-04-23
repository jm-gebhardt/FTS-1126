using Gebhardt.Acc.Common.Configurations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Configurations;

[TestClass]
public class MassTransitConfigurationTest
{
    [DataRow("BbYY&2^TxAAh6fr$", "amqp://user:BbYY%262%5ETxAAh6fr%24@host:port")]
    [DataRow("test", "amqp://user:test@host:port")]
    [DataTestMethod]
    public void Url_ValidData_UriWithHostPortUserPassword(string password, string url)
    {
        var config = new MassTransitConfiguration() { Host = "host", Password = password, Port = "port", User = "user" };
        config.Url.ShouldBe(url);
    }
}