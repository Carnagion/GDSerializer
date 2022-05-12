using System;
using System.Linq;

using Microsoft.CSharp;

namespace Godot.Serialization.Utility.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="Type"/>.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Checks if <paramref name="type"/> is the same as or inherits/implements <paramref name="genericType"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <param name="genericType">The generic <see cref="Type"/> definition to check with.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is or derives from <paramref name="genericType"/> regardless of whether <paramref name="type"/> is a concrete instantiation of it, else <see langword="false"/>.</returns>
        public static bool DerivesFromGenericType(this Type type, Type genericType)
        {
            if (type.GetInterfaces().Any(interfaceType => interfaceType.IsGenericType && (interfaceType.GetGenericTypeDefinition() == genericType)))
            {
                return true;
            }
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == genericType))
            {
                return true;
            }
            return type.BaseType?.DerivesFromGenericType(genericType) ?? false;
        }

        /// <summary>
        /// Checks if <paramref name="type"/> is the same as <paramref name="genericType"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <param name="genericType">The generic <see cref="Type"/> definition to check with.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is exactly <paramref name="genericType"/> or is a concrete instantiation of it, and <see langword="false"/> not if it is a different <see cref="Type"/> or inherits/implements <paramref name="genericType"/>.</returns>
        public static bool IsExactlyGenericType(this Type type, Type genericType)
        {
            try
            {
                return type == genericType.MakeGenericType(type.GenericTypeArguments);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the display name for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <returns>The full name of <paramref name="type"/>, as it would be displayed in source code (including namespace).</returns>
        public static string GetDisplayName(this Type type)
        {
            using CSharpCodeProvider provider = new();
            return provider.GetTypeOutput(new(type));
        }
    }
}