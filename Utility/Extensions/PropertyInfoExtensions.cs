using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Godot.Serialization;

namespace Godot.Utility.Extensions
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
            return property.CanRead
                   && property.CanWrite
                   && !property.GetIndexParameters().Any()
                   && property.GetCustomAttribute<CompilerGeneratedAttribute>() is null
                   && property.GetMethod.GetCustomAttribute<CompilerGeneratedAttribute>() is null
                   && !PropertyInfoExtensions.forbiddenTypes.Contains(property.PropertyType)
                   && (property.GetCustomAttribute<SerializeAttribute>()?.Serializable ?? true);
        }
    }
}