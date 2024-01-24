using System.Reflection;
using System.Xml.Linq;
using Gebhardt.StoreWare.Agv.Common.WebApi;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest;

[TestClass]
public class XmlDocResolverTest
{
    #region Test Data
    private const string XmlDocIn = @"<?xml version=""1.0"" encoding=""utf-8""?>
            <doc>
              <assembly>
               <name>Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest</name>
              </assembly>
              <members>
                <member name = ""T:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1"">
                  <summary>Interface1</summary>
                </member>
                <member name=""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1.Method1"">
                  <summary>Description for Method1</summary>
                </member>
                <member name = ""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1.Method2"">
                  <summary>Description for Method2</summary>
                </member>
                <member name=""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1.Method3"">
                  <summary>Description for Method3</summary>
                </member>
                <member name=""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1.CorrelationId"">
                  <summary>Used to identify a message as correlated so that the CorrelationId can be returned</summary>
                </member>
                <member name = ""T:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Class1"">
                  <summary>Class1</summary>
                </member>
                <member name=""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Class1.Method1"">
                  <summary>Description for <see cref=""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1.Method1""/></summary>
                </member>
                <member name = ""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Class1.Method2"">
                  <inheritdoc cref=""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1.Method2""/>
                </member>
                <member name = ""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Class1.Method3"">
                  <inheritdoc/>
                </member>
                <member name = ""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Class1.CorrelationId"">
                  <inheritdoc/>
                </member>
              </members>
            </doc>";

    private const string XmlDocOut = @"<?xml version=""1.0"" encoding=""utf-8""?>
            <doc>
              <assembly>
               <name>Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest</name>
              </assembly>
              <members>
                <member name = ""T:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1"">
                  <summary>Interface1</summary>
                </member>
                <member name=""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1.Method1"">
                  <summary>Description for Method1</summary>
                </member>
                <member name = ""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1.Method2"">
                  <summary>Description for Method2</summary>
                </member>
                <member name=""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1.Method3"">
                  <summary>Description for Method3</summary>
                </member>
                <member name=""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Interface1.CorrelationId"">
                  <summary>Used to identify a message as correlated so that the CorrelationId can be returned</summary>
                </member>
                <member name = ""T:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Class1"">
                  <summary>Class1</summary>
                </member>
                <member name=""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Class1.Method1"">
                  <summary>Description for Method1()</summary>
                </member>
                <member name = ""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Class1.Method2"">
                  <summary>Description for Method2</summary>
                </member>
                <member name = ""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Class1.Method3"">
                  <summary>Description for Method3</summary>
                </member>
                <member name = ""M:Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.XmlDocResolverTest+Class1.CorrelationId"">
                  <summary>Used to identify a message as correlated so that the CorrelationId can be returned</summary>
                </member>
              </members>
            </doc>";
    #endregion

    #region Test Members
    // ReSharper disable all
    /// <summary>Interface1</summary>
    private interface Interface1
    {
        /// <summary>Description for Method1</summary>
        int Method1();

        /// <summary>Description for Method2</summary>
        int Method2();

        /// <summary>Description for Method3</summary>
        int Method3();
    }

    /// <summary>Class1</summary>
    private class Class1 : Interface1, CorrelatedBy<Guid>
    {
        /// <summary>Description for <see cref="Interface1.Method1"/></summary>
        public int Method1()
        {
            return 0;
        }

        /// <inheritdoc cref="Interface1.Method2"/>
        public int Method2()
        {
            return 0;
        }

        /// <inheritdoc/>
        public int Method3()
        {
            return 0;
        }

        /// <inheritdoc/>
        public Guid CorrelationId { get; }
    }

    // ReSharper ensable all
    #endregion

    [TestMethod]
    public void RemoveDtoSuffixIfExists()
    {
        var xmlDocs = new List<XDocument>();
        var assemblies = new List<Assembly>();

        xmlDocs.Add(XDocument.Parse(XmlDocIn));
        assemblies.Add(Assembly.GetExecutingAssembly());

        IEnumerable<XDocument> resolvedXmlDocs = XmlDocResolver.ResolveInheritedDocs(xmlDocs, assemblies);

        var resolvedDoc = resolvedXmlDocs.First();
        var targetDoc = XDocument.Parse(XmlDocOut);

        resolvedDoc.ToString(SaveOptions.DisableFormatting).ShouldBe(targetDoc.ToString(SaveOptions.DisableFormatting));
    }
}