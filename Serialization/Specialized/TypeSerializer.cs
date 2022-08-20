using System;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for <see cref="Type"/>.
    /// </summary>
    public class TypeSerializer : ISerializer
    {
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="Type"/> to serialize.</param>
        /// <param name="type">The <see cref="Type"/> to serialize <paramref name="instance"/> as. This parameter is mostly ignored by the <see cref="TypeSerializer"/>.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public XmlNode Serialize(object instance, Type? type = null)
        {
            if (instance is not Type typeInstance)
            {
                throw new SerializationException(instance, $"\"{instance.GetType().GetDisplayName()}\" cannot be (de)serialized by {typeof(TypeSerializer).GetDisplayName()}");
            }
            XmlDocument context = new();
            XmlElement element = context.CreateElement(typeof(Type).GetDisplayName());
            element.AppendChild(context.CreateTextNode(typeInstance.GetDisplayName()));
            return element;
        }
        
        /// <summary>
        /// Deserializes <paramref name="node"/> into an <see cref="object"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <param name="type">The <see cref="Type"/> of <see cref="object"/> to deserialize <paramref name="node"/> as. This parameter is mostly ignored by the <see cref="TypeSerializer"/>.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if a <see cref="Type"/> could not be inferred from <paramref name="node"/> or was invalid, or <paramref name="node"/> could not be deserialized due to unexpected errors or invalid data.</exception>
        public object? Deserialize(XmlNode node, Type? type = null)
        {
            type ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            if (type != typeof(Type))
            {
                throw new SerializationException(node, $"\"{type.GetDisplayName()}\" cannot be (de)serialized by {typeof(TypeSerializer).GetDisplayName()}");
            }
            return node.ChildNodes.Count is 1 && node.ChildNodes[0] is XmlText text
                ? text.InnerText
                    .Replace("&lt;", "<")
                    .Replace("&gt;", ">")
                    .Typeof()
                : throw new SerializationException(node, "Node contains invalid number or type of child nodes");
        }
    }
}