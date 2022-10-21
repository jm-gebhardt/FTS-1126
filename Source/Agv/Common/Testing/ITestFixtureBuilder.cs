
namespace Gebhardt.StoreWare.Agv.Common.Testing
{
    /// <summary>
    /// Marker interface for test fixture builders. Used to prevent using of configured, but unbuilt test fixtures
    /// </summary>
    public interface ITestFixtureBuilder<out T> where T : class
    {
        /// <summary>
        /// Builds the configured test fixture
        /// </summary>
        /// <returns>The built test fixture</returns>
        T Build();
    }
}
