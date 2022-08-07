using System;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for <see cref="XmlNode"/>s.
    /// </summary>
    public class XmlNodeSerializer : ISerializer
    {
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize. It must inherit from <see cref="XmlNode"/>.</param>
        /// <param name="type">The <see cref="Type"/> to serialize <paramref name="instance"/> as.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public XmlNode Serialize(object instance, Type? type = null)
        {
            type ??= instance.GetType();
            if (!typeof(XmlNode).IsAssignableFrom(type) || instance is not XmlNode node)
            {
                throw new SerializationException(instance, $"\"{type.GetDisplayName()}\" cannot be (de)serialized by {typeof(XmlNodeSerializer).GetDisplayName()}");
            }
            XmlDocument context = new();
            XmlElement element = context.CreateElement(type.GetDisplayName());
            element.AppendChild(context.ImportNode(node, true));
            return element;
        }
        
        /// <summary>
        /// Deserializes <paramref name="node"/> into an <see cref="object"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <param name="type">The <see cref="Type"/> of <see cref="object"/> to deserialize <paramref name="node"/> as.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if a <see cref="Type"/> could not be inferred from <paramref name="node"/> or was invalid, or <paramref name="node"/> could not be deserialized due to unexpected errors or invalid data.</exception>
        public object? Deserialize(XmlNode node, Type? type = null)
        {
            type ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            if (!typeof(XmlNode).IsAssignableFrom(type))
            {
                throw new SerializationException(node, $"\"{type.GetDisplayName()}\" cannot be (de)serialized by {typeof(XmlNodeSerializer).GetDisplayName()}");
            }
            return node.ChildNodes.Count is 1
                ? node.ChildNodes[0]
                : throw new SerializationException(node, "Node contains invalid number of child nodes");
        }
    }
}