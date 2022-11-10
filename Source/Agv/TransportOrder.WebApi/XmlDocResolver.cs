using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi
{
    /// <summary>
    /// Helper class to resolve inherited comments in xml docs
    /// </summary>
    public static class XmlDocResolver
    {
        /// <summary>
        /// Resolve inheritdoc comments
        /// <para>
        /// Based on: https://github.com/domaindrivendev/Swashbuckle.WebApi/issues/1000#issuecomment-821813257
        /// </para>
        /// </summary>
        public static IEnumerable<XDocument> ResolveInheritedDocs(List<XDocument> xmlDocs, List<Assembly> assemblies)
        {
            // need a map for looking up member elements by name.
            var targetMemberElements = new Dictionary<string, XElement>();

            // add member elements across all XML docs to the look-up table. We want <member> elements
            // that have a 'name' attribute but don't contain an <inheritdoc> child element.
            foreach (var doc in xmlDocs)
            {
                IEnumerable<XElement> members = doc.XPathSelectElements("/doc/members/member[@name and not(inheritdoc)]");

                foreach (var m in members)
                {
                    targetMemberElements.Add(m.Attribute("name")!.Value, m);
                }
            }

            // for each <member> element that has an <inheritdoc> child element which references another
            // <member> element, replace the <inheritdoc> element with the nodes of the referenced <member>
            // element (effectively this 'dereferences the pointer' which is something Swagger doesn't support).
            foreach (var doc in xmlDocs)
            {
                IEnumerable<XElement> pointerMembers = doc.XPathSelectElements("/doc/members/member[inheritdoc]");

                foreach (var pointerMember in pointerMembers)
                {
                    var pointerElement = pointerMember.Element("inheritdoc")!;
                    string? targetReference = pointerElement.Attribute("cref")?.Value;
                    string nameAttribute = pointerMember.Attribute("name")!.Value;

                    //  No cref -> get referenced member via reflection
                    targetReference ??= TryGetTargetReference(nameAttribute, assemblies);

                    if (targetReference == null)
                    {
                        // Some comments are inherited from external assemblies
                        string? externalDoc = GetExternalDoc(nameAttribute);
                        if (externalDoc != null)
                        {
                            pointerElement.ReplaceWith(new XElement("summary", externalDoc));
                        }
                    }
                    else if (targetMemberElements.TryGetValue(targetReference, out var targetMember))
                    {
                        pointerElement.ReplaceWith(targetMember.Nodes());
                    }
                }
            }

            // replace all <see> elements with the unqualified member name that they point to (Swagger uses the
            // fully qualified name which makes no sense because the relevant classes and namespaces are not useful
            // when calling an API over HTTP).
            foreach (var doc in xmlDocs)
            {
                foreach (var seeElement in doc.XPathSelectElements("//see[@cref]"))
                {
                    string targetMemberName = seeElement.Attribute("cref")!.Value;
                    string shortMemberName = targetMemberName.Substring(targetMemberName.LastIndexOf('.') + 1);

                    if (targetMemberName.StartsWith("M:"))
                    {
                        shortMemberName += "()";
                    }

                    seeElement.ReplaceWith(shortMemberName);
                }
            }

            return xmlDocs;
        }

        private static string? TryGetTargetReference(string nameAttribute, List<Assembly> assemblies)
        {
            int lastDotIndex = nameAttribute.LastIndexOf(".", StringComparison.InvariantCulture);
            string memberName = nameAttribute.Substring(lastDotIndex + 1);
            string typeName = nameAttribute.Substring(2, lastDotIndex - 2);
            string prefix = nameAttribute.Substring(0, 2);

            foreach (var assembly in assemblies)
            {
                string? assemblyName = assembly.GetName().Name;
                if (assemblyName == null || !typeName.StartsWith(assemblyName))
                {
                    continue;
                }

                var memberType = assembly.GetType(typeName);
                if (memberType != null)
                {
                    string? parentType = GetMemberBaseType(memberType, memberName);
                    if (parentType != null)
                    {

                        return prefix + parentType + "." + memberName;
                    }
                }
            }

            return null;
        }

        private static string? GetMemberBaseType(Type memberType, string memberName)
        {
            IEnumerable<Type> parents = GetParentTypes(memberType);

            foreach (var parent in parents)
            {
                // Only look for our own parent types
                if (!parent.Namespace!.StartsWith("Gebhardt.StoreWare.Agv"))
                {
                    continue;
                }

                foreach (var parentMember in parent.GetMembers())
                {
                    if (parentMember.Name == memberName)
                    {
                        return parent.FullName;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<Type> GetParentTypes(Type type)
        {
            // return all implemented or inherited interfaces
            foreach (var i in type.GetInterfaces())
            {
                yield return i;
            }

            // return all inherited types
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
        }

        private static string? GetExternalDoc(string nameAttribute)
        {
            int lastDotIndex = nameAttribute.LastIndexOf(".", StringComparison.InvariantCulture);
            string memberName = nameAttribute.Substring(lastDotIndex + 1);

            // Resolving xml docs for external assemblies would require to include the xml docs of all assemblies
            // This seems overkill, so we use a manual lookup-table for commonly used types instead
            if (memberName == "CorrelationId")
            {
                return "Used to identify a message as correlated so that the CorrelationId can be returned";
            }

            return null;
        }
    }
}
