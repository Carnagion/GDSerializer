using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Godot.Serialization.Utility.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="PropertyInfo"/>.
    /// </summary>
    public static class PropertyInfoExtensions
    {
        private static readonly Type[] forbiddenTypes = {typeof(Pointer), typeof(IntPtr),};
        
        /// <summary>
        /// Determines if <paramref name="property"/> is a (de)serializable property.
        /// </summary>
        /// <param name="property">The <see cref="PropertyInfo"/> to check.</param>
        /// <returns><see langword="true"/> if <paramref name="property"/> can be (de)serialized by an <see cref="ISerializer"/>.</returns>
        public static bool IsSerializable(this PropertyInfo property)
        {
            if (!property.CanRead || !property.CanWrite)
            {
                return false;
            }
            if (property.GetIndexParameters().Any())
            {
                return false;
            }
            if (property.GetCustomAttribute<CompilerGeneratedAttribute>() is not null || property.GetMethod.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
            {
                return false;
            }
            if (PropertyInfoExtensions.forbiddenTypes.Contains(property.PropertyType))
            {
                return false;
            }
            return property.GetCustomAttribute<SerializeAttribute>()?.Serializable ?? true;
        }
    }
}