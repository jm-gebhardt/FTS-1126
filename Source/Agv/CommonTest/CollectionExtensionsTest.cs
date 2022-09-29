using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Extensions;

namespace Gebhardt.StoreWare.Agv.CommonTest
{
    /// <summary>
    /// Test <see cref="Common.Extensions.CollectionExtensions.AddRange{T}(HashSet{T}, IEnumerable{T})"/>.
    /// </summary>
    [TestClass]
    public class CollectionExtensionsTest
    {
        /// <summary>
        /// Test xxx for <see cref="HashSet{T}"/>.
        /// </summary>
        [TestMethod]
        public void HashSet_AddRange()
        {
            var set = new HashSet<string>();
            var list = new List<string>();

            Assert.IsTrue(set.Add("1"));
            Assert.AreEqual(1, set.Count);
            list.Add("2");
            list.Add("3");
            Assert.IsTrue(set.AddRange(list));
            Assert.AreEqual(3, set.Count);
            Assert.IsFalse(set.AddRange(list));
            Assert.AreEqual(3, set.Count);
        }
    }
}
