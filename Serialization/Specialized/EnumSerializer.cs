using System;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for enums.
    /// </summary>
    public class EnumSerializer : ISerializer
    {
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize. It must be an array.</param>
        /// <param name="enumType">The <see cref="Type"/> to serialize <paramref name="instance"/> as. It must be an enum type.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public XmlNode Serialize(object instance, Type? enumType = null)
        {
            enumType ??= instance.GetType();
            if (!enumType.IsEnum)
            {
                throw new SerializationException(instance, $"\"{enumType.GetDisplayName()}\" cannot be serialized by {typeof(EnumerableSerializer).GetDisplayName()}");
            }
            XmlDocument context = new();
            XmlElement element = context.CreateElement(enumType.GetDisplayName());
            element.AppendChild(context.CreateTextNode(instance.ToString()));
            return element;
        }
        
        /// <summary>
        /// Deserializes <paramref name="node"/> into an <see cref="object"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <param name="enumType">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as. It must be an enum type.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if a <see cref="Type"/> could not be inferred from <paramref name="node"/> or was invalid, or <paramref name="node"/> could not be deserialized due to unexpected errors or invalid data.</exception>
        public object Deserialize(XmlNode node, Type? enumType = null)
        {
            enumType ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            if (!enumType.IsEnum)
            {
                throw new SerializationException(node, $"\"{enumType.GetDisplayName()}\" cannot be deserialized by {typeof(EnumerableSerializer).GetDisplayName()}");
            }
            return node.ChildNodes.Count is 1 && node.ChildNodes[0] is XmlText text
                ? Enum.Parse(enumType, text.InnerText, false)
                : throw new SerializationException(node, "Node contains invalid number or type of child nodes");
        }
    }
}