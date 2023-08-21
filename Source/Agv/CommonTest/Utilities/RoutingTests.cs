using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.CommonTest.Utilities;

[TestClass]
public class RoutingTests
{
    /// <summary>
    /// Cut valid routes into partial routes.
    /// </summary>
    /// <remarks>
    /// All input list data is comma separated string list.
    /// </remarks>
    /// <param name="nodes">List of nodes of the complete route.</param>
    /// <param name="source">Source node.</param>
    /// <param name="destination">Destination node.</param>
    /// <param name="expected">Expected list of nodes after cutting.</param>
    [TestMethod]
    [DataRow(    "S,1,2,3,D",     null, null, "S,1,2,3,D")]
    [DataRow(    "S,1,2,3,D",     null, "D",  "S,1,2,3,D")]
    [DataRow(    "S,1,2,3,D",     "S",  null, "S,1,2,3,D")]
    [DataRow(    "S,1,2,3,D",     "S",  "D",  "S,1,2,3,D")]
    [DataRow("a,b,S,1,2,3,D",     "S",  null, "S,1,2,3,D")]
    [DataRow("a,b,S,1,2,3,D",     "S",  "D",  "S,1,2,3,D")]
    [DataRow(    "S,1,2,3,D,x,y", null, "D",  "S,1,2,3,D")]
    [DataRow(    "S,1,2,3,D,x,y", "S",  "D",  "S,1,2,3,D")]
    [DataRow(    "S,D",           null, null, "S,D")]
    [DataRow(    "S,D",           null, "D",  "S,D")]
    [DataRow(    "S,D",           "S",  null, "S,D")]
    [DataRow(    "S,D",           "S",  "D",  "S,D")]
    [DataRow(  "D,2,1,S,1,2,D",   null, null, "D,2,1,S,1,2,D")]
    [DataRow(  "D,2,1,S,1,2,D",   null, "D",  "D")]
    [DataRow(  "D,2,1,S,1,2,D",   "S",  null, "S,1,2,D")]
    [DataRow(  "D,2,1,S,1,2,D",   "S",  "D",  "S,1,2,D")]
    [DataRow(  "D,2,1,S",         "D",  "D",  "D")]
    public void CutNodes_ValidInput_NoExceptions(string nodes, string? source, string? destination, string expected)
    {
        string[] result = RoutingHelper.CutNodes(nodes.Split(','), source, destination);
        string.Join(',', result).ShouldBe(expected);
    }

    /// <summary>
    /// Test invalid input.
    /// </summary><remarks>
    /// All input list data is comma separated string list.
    /// </remarks>
    /// <param name="nodes">List of nodes of the complete route.</param>
    /// <param name="source">Source node.</param>
    /// <param name="destination">Destination node.</param>
    [TestMethod]
    [DataRow("",          null, null)]
    [DataRow("S,1,2,3,D", "X",  null)]
    [DataRow("S,1,2,3,D", null, "Y" )]
    [DataRow("S,1,2,3,D", "D",  "S" )]
    public void CutNodes_InvalidInput_ExceptionExpected(string nodes, string? source, string? destination)
    {
        Should.Throw<ArgumentException>(() =>
        {
            RoutingHelper.CutNodes(nodes.Split(',', StringSplitOptions.RemoveEmptyEntries), source, destination);
        });
    }
}