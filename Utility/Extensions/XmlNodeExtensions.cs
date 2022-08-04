using System;
using System.Xml;

namespace Godot.Utility.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="XmlNode"/>.
    /// </summary>
    public static class XmlNodeExtensions
    {
        /// <summary>
        /// Tries to find a suitable <see cref="Type"/> to deserialize <paramref name="node"/> as.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <returns>The <see cref="Type"/> of the serialized data in <paramref name="node"/>, or <see langword="null"/> if no suitable <see cref="Type"/> was found.</returns>
        public static Type? GetTypeToDeserialize(this XmlNode node)
        {
            // Use Replace() instead of XMLUnescape() which throws an exception for some reason
            return (node.Attributes?["Type"]?.InnerText ?? node.Name)
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Typeof();
        }
    }
}