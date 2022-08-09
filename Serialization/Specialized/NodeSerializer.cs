using System;
using System.Linq;
using System.Reflection;
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
        /// <param name="defaultSerializer">The <see cref="Serializer"/> to use when (de)serializing a <see cref="Node"/>'s members.</param>
        public NodeSerializer(Serializer defaultSerializer) : base(defaultSerializer)
        {
        }
        
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The instance to serialize. It must be a <see cref="Node"/>.</param>
        /// <param name="nodeType">The <see cref="Type"/> to serialize <paramref name="instance"/> as. It must be or inherit from <see cref="Node"/>.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public override XmlNode Serialize(object instance, Type? nodeType = null)
        {
            nodeType ??= instance.GetType();
            if (!typeof(Node).IsAssignableFrom(nodeType) || instance is not Node nodeInstance)
            {
                throw new SerializationException(instance, $"\"{nodeType.GetDisplayName()}\" cannot be (de)serialized by {typeof(NodeSerializer).GetDisplayName()}");
            }
            
            XmlDocument context = new();
            
            // Use the "Type" attribute if generic or nested type as ` and + are not allowed as XML node names
            XmlElement element;
            if (nodeType.IsGenericType)
            {
                element = context.CreateElement("Generic");
                element.SetAttribute("Type", nodeType.GetDisplayName().XMLEscape());
            }
            else if (nodeType.IsNested)
            {
                element = context.CreateElement("Nested");
                element.SetAttribute("Type", nodeType.GetDisplayName().XMLEscape());
            }
            else
            {
                element = context.CreateElement(nodeType.GetDisplayName());
            }
            
            Serializer defaultSerializer = (Serializer)this.ItemSerializer;
            defaultSerializer.SerializeMembers(nodeInstance, nodeType).ForEach(pair => element.AppendChild(context.ImportNode(pair.Item1, true)));
            
            if (nodeInstance.GetChildCount() is 0)
            {
                return element;
            }
            
            XmlElement childrenElement = context.CreateElement("Children");
            element.AppendChild(childrenElement);
            this.SerializeItems(nodeInstance.GetChildren().Cast<Node>(), typeof(Node)).ForEach(node => childrenElement.AppendChild(context.ImportNode(node, true)));
            
            return element;
        }
        
        /// <summary>
        /// Deserializes <paramref name="node"/> into a <see cref="Node"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <param name="nodeType">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as. It must be or inherit from <see cref="Node"/>.</param>
        /// <returns>A <see cref="Node"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="node"/> could not be deserialized due to unexpected errors or invalid input.</exception>
        public override object Deserialize(XmlNode node, Type? nodeType = null)
        {
            nodeType ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            if (!typeof(Node).IsAssignableFrom(nodeType))
            {
                throw new SerializationException(node, $"\"{nodeType.GetDisplayName()}\" cannot be (de)serialized by {typeof(NodeSerializer).GetDisplayName()}");
            }
            
            XmlNode? childrenElement = node.SelectSingleNode("Children");
            if (childrenElement is not null)
            {
                node.RemoveChild(childrenElement);
            }
            
            Serializer defaultSerializer = (Serializer)this.ItemSerializer;
            
            object instance = Activator.CreateInstance(nodeType, true) ?? throw new SerializationException(node, $"Unable to instantiate {nodeType.GetDisplayName()}");
            foreach ((object? value, MemberInfo member) in defaultSerializer.DeserializeMembers(node, nodeType))
            {
                switch (member)
                {
                    case PropertyInfo property:
                        property.SetValue(instance, value);
                        break;
                    case FieldInfo field:
                        field.SetValue(instance, value);
                        break;
                }
            }
            
            if (childrenElement is not null)
            {
                Node nodeInstance = (Node)instance;
                this.DeserializeItems(childrenElement, typeof(Node))
                    .Cast<Node>()
                    .ForEach(child => nodeInstance.AddChild(child));
            }
            
            return instance;
        }
    }
}