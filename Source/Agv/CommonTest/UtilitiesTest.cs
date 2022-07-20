using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.CommonTest
{
    /// <summary>
    /// Test for utility <see cref="StringExtensions"/>.
    /// </summary>
    [TestClass]
    public class UtilitiesTest
    {
        private const string TestPattern = "#HashMe#";
        private const string HashPattern = "a53178cb-6f56-b8b8-638e-3a2447004b78";

        /// <summary>
        /// Start TestWorker with no arguments provided
        /// This is a very lame integration test that does not wait for the actual services to get started
        /// </summary>
        [TestMethod]
        public void String_ToGuid()
        {
            var expected = new Guid(HashPattern);
            var current = TestPattern.ToGuid();
            Assert.AreEqual(expected, current);
            current = TestPattern.ToGuid();
            Assert.AreEqual(expected, current);
        }
    }
}
