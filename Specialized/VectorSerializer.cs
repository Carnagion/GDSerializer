using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

using Godot.Serialization.Utility.Exceptions;
using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for <see cref="Vector2"/> and <see cref="Vector3"/>.
    /// </summary>
    public class VectorSerializer : ISerializer
    {
        private static readonly Dictionary<Type, Regex> vectorRegexes = new(2)
        {
            {typeof(Vector2), new(@"\(\s*(?<x>[+-]?\d+(?:\.\d+)?)\s*,\s*(?<y>[+-]?\d+(?:\.\d+)?)\s*\)")},
            {typeof(Vector3), new(@"\(\s*(?<x>[+-]?\d+(?:\.\d+)?)\s*,\s*(?<y>[+-]?\d+(?:\.\d+)?)\s*,\s*(?<z>[+-]?\d+(?:\.\d+)?)\s*\)")},
        };

        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize. It must be a <see cref="Vector2"/> or <see cref="Vector3"/>.</param>
        /// <param name="vectorType">The <see cref="Type"/> to serialize <paramref name="instance"/> as. This parameter is ignored by <see cref="VectorSerializer"/>.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> is not a <see cref="Vector2"/> or <see cref="Vector3"/>.</exception>
        public XmlNode Serialize(object instance, Type? vectorType = null)
        {
            XmlDocument context = new();
            switch (instance)
            {
                case Vector2 vector2:
                    XmlElement element2 = context.CreateElement(typeof(Vector2).FullName);
                    element2.AppendChild(context.CreateTextNode($"({vector2.x}, {vector2.y})"));
                    return element2;
                case Vector3 vector3:
                    XmlElement element3 = context.CreateElement(typeof(Vector3).FullName);
                    element3.AppendChild(context.CreateTextNode($"({vector3.x}, {vector3.y}, {vector3.z})"));
                    return element3;
                default:
                    throw new SerializationException(instance, $"\"{instance.GetType().GetDisplayName()}\" cannot be serialized by {typeof(VectorSerializer).GetDisplayName()}");
            }
        }

        /// <summary>
        /// Deserializes <paramref name="node"/> into an <see cref="object"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <param name="type">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as. It must be one of <see cref="Vector2"/> or <see cref="Vector3"/>.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="type"/> is invalid or if <paramref name="node"/> contains no or invalid data.</exception>
        public object Deserialize(XmlNode node, Type? type = null)
        {
            if (type is null)
            {
                throw new SerializationException(node, $"No {nameof(type)} provided");
            }
            
            if (!node.HasChildNodes)
            {
                throw new SerializationException(node, "Node contains no textual data");
            }

            try
            {
                string text = node.ChildNodes[0].InnerText.Trim();
                Match match = VectorSerializer.vectorRegexes[type].Match(text);
                if (!match.Success)
                {
                    throw new SerializationException(node, "Invalid vector format");
                }

                float x = Single.Parse(match.Groups["x"].Value);
                float y = Single.Parse(match.Groups["y"].Value);
                if (type == typeof(Vector2))
                {
                    return new Vector2(x, y);
                }
                float z = Single.Parse(match.Groups["z"].Value);
                return new Vector3(x, y, z);
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }
    }
}