using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence
{
    /// <summary>
    /// EntityTypeBuilder extension 
    /// </summary>
    public static class EntityTypeBuilderExtensions
    {
        /// <summary>
        /// Configures all entity properties of T, except the one listed in filter.
        /// 
        /// Source <see href="https://stackoverflow.com/a/48278685" />
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="filter"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public static List<PropertyBuilder> ConfigureAllProperties<T>(this EntityTypeBuilder<T> builder, 
            Func<PropertyInfo, bool>? filter = null) where T : class
        {
            IEnumerable<PropertyInfo> properties = typeof(T)
                .GetProperties()
                .AsEnumerable();

            if (filter != null)
            {
                properties = properties
                    .Where(filter);
            }

            return properties
                .Select(x => builder.Property(x.PropertyType, x.Name))
                .ToList();
        }
    }
}