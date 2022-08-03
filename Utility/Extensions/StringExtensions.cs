using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Godot.Utility.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="string"/>.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Searches for a <see cref="Type"/> whose C# display name matches <paramref name="typeName"/>.
        /// </summary>
        /// <param name="typeName">The name of the <see cref="Type"/> to find.</param>
        /// <returns>The corresponding <see cref="Type"/>, or <see langword="null"/> if none was found.</returns>
        public static Type? Typeof(this string typeName)
        {
            Regex typeNameRegex = new(@"^(?<name>[a-zA-Z0-9_\.]+)(<(?<parameters>[a-zA-Z0-9_,\s\<\>\[\]\.]*)>)?(\[(?<array>[\s,]*)\])?$", RegexOptions.Compiled);
            
            Match match = typeNameRegex.Match(typeName);
            if (!match.Success)
            {
                return null;
            }
            
            Group name = match.Groups["name"];
            Group parameters = match.Groups["parameters"];
            Group array = match.Groups["array"];

            if (array.Success)
            {
                int rank = array.Value.Count(character => character is ',') + 1;
                string elementTypeName = StringExtensions.GetArrayElementTypeName(typeName);
                return elementTypeName.Typeof()?.MakeArrayType(rank);
            }

            if (parameters.Success)
            {
                string[] genericParameterTypeNames = StringExtensions.GetGenericParameterTypeNames(parameters.Value).ToArray();
                int length = genericParameterTypeNames.Length - 1;
                string commas = length is 0 ? "" : IEnumerableExtensions.Generate(length, _ => ", ")
                    .Aggregate(new StringBuilder(), (builder, comma) => builder.Append(comma))
                    .ToString();
                Type? unboundGenericType = StringExtensions.FindType($"{name.Value}<{commas}>");
                if (unboundGenericType is null || genericParameterTypeNames.All(String.IsNullOrWhiteSpace))
                {
                    return unboundGenericType;
                }
                Type?[] genericParameterTypes = Array.ConvertAll(genericParameterTypeNames, StringExtensions.Typeof);
                return genericParameterTypes.Any(genericParameterType => genericParameterType is null) ? null : unboundGenericType.MakeGenericType(genericParameterTypes);
            }

            return StringExtensions.FindType(typeName);
        }

        private static Type? FindType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Distinct()
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type => type.GetDisplayName() == typeName);
        }
        
        private static string GetArrayElementTypeName(string typeName)
        {
            int arrayCloseBracketIndex = typeName
                .Reverse()
                .IndexOf('[');
            string elementTypeName = typeName[..^(arrayCloseBracketIndex + 1)];
            return elementTypeName;
        }
        
        private static IEnumerable<string> GetGenericParameterTypeNames(string parameters)
        {
            StringBuilder builder = new();
            int level = 0;
            foreach (char character in parameters)
            {
                if (character is ',' && level is 0)
                {
                    yield return builder.ToString();
                    builder.Clear();
                }
                else
                {
                    builder.Append(character);
                }
                level += character switch
                {
                    '<' => 1,
                    '>' => -1,
                    _ => 0,
                };
            }
            yield return builder.ToString();
        }
    }
}