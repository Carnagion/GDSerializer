using System;
using System.Linq;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for <see cref="Node"/> and types that inherit from it.
    /// </summary>
    public class NodeSerializer : CollectionSerializer
    {
        /// <summary>
        /// Initialises a new <see cref="NodeSerializer"/> with the specified parameters.
        /// </summary>
        /// <param name="defaultSerializer">The serializer to use when (de)serializing the <see cref="Node"/>'s fields and properties.</param>
        public NodeSerializer(ISerializer defaultSerializer) : base(defaultSerializer)
        {
        }
        
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize. It must be or inherit from <see cref="Node"/>.</param>
        /// <param name="nodeType">The <see cref="Type"/> to serialize <paramref name="instance"/> as.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public override XmlNode Serialize(object instance, Type? nodeType = null)
        {
            nodeType ??= instance.GetType();
            if (instance is not Node node)
            {
                throw new SerializationException(instance, $"\"{nodeType.GetDisplayName()}\" cannot be (de)serialized by {typeof(NodeSerializer).GetDisplayName()}");
            }
            
            XmlNode element = this.ItemSerializer.Serialize(instance, nodeType);
            
            try
            {
                if (node.GetChildCount() is 0)
                {
                    return element;
                }
                XmlDocument context = element.OwnerDocument!;
                XmlElement childrenElement = context.CreateElement("Children");
                this.SerializeItems(node.GetChildren().Cast<Node>(), typeof(Node)).ForEach(childElement => childrenElement.AppendChild(context.ImportNode(childElement, true)));
                return element;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(instance, exception);
            }
        }
        
        /// <summary>
        /// Deserializes <paramref name="node"/> into an <see cref="object"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <param name="nodeType">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as. It must be or inherit from <see cref="Node"/>.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="node"/> could not be deserialized due to unexpected errors or invalid input.</exception>
        public override object? Deserialize(XmlNode node, Type? nodeType = null)
        {
            XmlNode? childrenElement = node.SelectSingleNode("Children");
            if (childrenElement is null)
            {
                return this.ItemSerializer.Deserialize(node, nodeType);
            }
            
            nodeType ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            if (!typeof(Node).IsAssignableFrom(nodeType))
            {
                throw new SerializationException(node, $"\"{nodeType.GetDisplayName()}\" cannot be (de)serialized by {typeof(NodeSerializer).GetDisplayName()}");
            }
            
            childrenElement = node.RemoveChild(childrenElement);
            
            try
            {
                object? instance = this.ItemSerializer.Deserialize(node);
                if (instance is Node nodeInstance)
                {
                    this.DeserializeItems(childrenElement, typeof(Node))
                        .Cast<Node>()
                        .ForEach(child => nodeInstance.AddChild(child));
                }
                return instance;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }
    }
}