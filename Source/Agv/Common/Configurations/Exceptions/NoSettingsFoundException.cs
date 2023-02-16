using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.Exceptions
{
    /// <summary>
    /// Settings were not found by given key
    /// </summary>
    public class NoSettingsFoundException : Exception
    {
        /// <summary>
        /// The key which was not found
        /// </summary>
        [UsedImplicitly]
        public string Key { get; }

        /// <summary>
        /// The settings type which were not found
        /// </summary>
        [UsedImplicitly]
        public string TypeName { get; }

        /// <inheritdoc cref="NoSettingsFoundException"/>
        public NoSettingsFoundException(string key, string typeName)
        {
            Key = key;
            TypeName = typeName;
        }
    }
}
