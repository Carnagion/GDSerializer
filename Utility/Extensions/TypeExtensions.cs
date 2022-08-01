using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CSharp;

namespace Godot.Utility.Extensions
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
        
        /// <summary>
        /// Retrieves all members defined in <paramref name="type"/> as well as its base <see cref="Type"/>s using <paramref name="flags"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to search in.</param>
        /// <param name="flags">The binding constraints.</param>
        /// <typeparam name="T">The <see cref="Type"/> of <see cref="MemberInfo"/> to retrieve.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="MemberInfo"/> defined in <paramref name="type"/> and its base <see cref="Type"/>s.</returns>
        public static IEnumerable<T> GetAllMembers<T>(this Type type, BindingFlags flags = BindingFlags.Default) where T : MemberInfo
        {
            IEnumerable<T> members = type.GetMembers(flags).OfType<T>();
            return type.BaseType is null ? members : members.Concat(type.BaseType.GetAllMembers<T>(flags));
        }
        
        /// <summary>
        /// Searches for the specified field in <paramref name="type"/> and its base <see cref="Type"/>s.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to search in.</param>
        /// <param name="name">The name of the field.</param>
        /// <param name="flags">The binding constraints.</param>
        /// <returns>A <see cref="FieldInfo"/> representing the field matching the specified name and constraints, or <see langword="null"/> if no match was found.</returns>
        public static FieldInfo? FindField(this Type type, string name, BindingFlags flags = BindingFlags.Default)
        {
            return type.GetField(name, flags) ?? type.BaseType?.FindField(name, flags);
        }
        
        /// <summary>
        /// Searches for the specified property in <paramref name="type"/> and its base <see cref="Type"/>s.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to search in.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="flags">The binding constraints.</param>
        /// <returns>A <see cref="PropertyInfo"/> representing the property matching the specified name and constraints, or <see langword="null"/> if no match was found.</returns>
        public static PropertyInfo? FindProperty(this Type type, string name, BindingFlags flags = BindingFlags.Default)
        {
            return type.GetProperty(name, flags) ?? type.BaseType?.FindProperty(name, flags);
        }
    }
}