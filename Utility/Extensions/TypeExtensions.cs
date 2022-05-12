using System;
using System.Linq;

using Microsoft.CSharp;

namespace Godot.Serialization.Utility.Extensions
{
    public static class TypeExtensions
    {
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

        public static string GetDisplayName(this Type type)
        {
            using CSharpCodeProvider provider = new();
            return provider.GetTypeOutput(new(type));
        }
    }
}