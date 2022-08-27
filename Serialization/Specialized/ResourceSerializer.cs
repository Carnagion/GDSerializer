using System;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for <see cref="Resource"/> and types that inherit from it.
    /// </summary>
    public class ResourceSerializer : ISerializer
    {
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The instance to serialize. It must be a <see cref="Resource"/>.</param>
        /// <param name="resourceType">The <see cref="Type"/> to serialize <paramref name="instance"/> as. It must be or inherit from <see cref="Resource"/>.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public XmlNode Serialize(object instance, Type? resourceType = null)
        {
            resourceType ??= instance.GetType();
            if (!typeof(Resource).IsAssignableFrom(resourceType) || instance is not Resource resource)
            {
                throw new SerializationException(instance, $"\"{resourceType.GetDisplayName()}\" cannot be (de)serialized by {typeof(ResourceSerializer).GetDisplayName()}");
            }
            
            XmlDocument context = new();
            XmlElement element = context.CreateElement("Resource");
            element.SetAttribute("Type", resourceType.GetDisplayName());
            element.AppendChild(context.CreateTextNode(resource.ResourcePath));
            return element;
        }
        
        /// <summary>
        /// Deserializes <paramref name="node"/> into a <see cref="Resource"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <param name="resourceType">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as. It must be or inherit from <see cref="Resource"/>.</param>
        /// <returns>A <see cref="Node"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="node"/> could not be deserialized due to unexpected errors or invalid input.</exception>
        public object? Deserialize(XmlNode node, Type? resourceType = null)
        {
            resourceType ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            if (!typeof(Resource).IsAssignableFrom(resourceType))
            {
                throw new SerializationException(node, $"\"{resourceType.GetDisplayName()}\" cannot be (de)serialized by {typeof(ResourceSerializer).GetDisplayName()}");
            }
            
            if (node.ChildNodes.Count is not 1 || node.ChildNodes[0] is not XmlText textNode)
            {
                throw new SerializationException(node, "Node contains invalid number or type of child nodes");
            }
            
            return GD.Load(textNode.InnerText);
        }
    }
}