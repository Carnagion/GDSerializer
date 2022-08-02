using System;
using System.Linq;
using System.Reflection;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    internal class NodeSerializer : CollectionSerializer
    {
        public NodeSerializer(Serializer defaultSerializer) : base(defaultSerializer)
        {
            this.defaultSerializer = defaultSerializer;
        }
        
        private readonly Serializer defaultSerializer;
        
        public override XmlNode Serialize(object instance, Type? nodeType = null)
        {
            nodeType ??= instance.GetType();
            if (!typeof(Node).IsAssignableFrom(nodeType))
            {
                throw new SerializationException(instance, $"\"{nodeType.GetDisplayName()}\" cannot be (de)serialized by {typeof(NodeSerializer).GetDisplayName()}");
            }
            
            XmlDocument context = new();
            
            // Use the "Type" attribute if generic or nested type as ` and + are not allowed as XML node names
            XmlElement element;
            if (nodeType.IsGenericType)
            {
                element = context.CreateElement("Generic");
                element.SetAttribute("Type", nodeType.FullName);
            }
            else if (nodeType.IsNested)
            {
                element = context.CreateElement("Nested");
                element.SetAttribute("Type", nodeType.FullName);
            }
            else
            {
                element = context.CreateElement(nodeType.GetDisplayName());
            }
            
            this.defaultSerializer.SerializeMembers(instance, nodeType).ForEach(pair => element.AppendChild(context.ImportNode(pair.Item1, true)));
            
            Node nodeInstance = (Node)instance;
            if (nodeInstance.GetChildCount() is 0)
            {
                return element;
            }
            
            XmlElement childrenElement = context.CreateElement("Children");
            element.AppendChild(childrenElement);
            this.SerializeItems(((Node)instance).GetChildren().Cast<Node>(), typeof(Node)).ForEach(node => childrenElement.AppendChild(context.ImportNode(node, true)));
            
            return element;
        }
        
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
            
            object instance = Activator.CreateInstance(nodeType, true) ?? throw new SerializationException(node, $"Unable to instantiate {nodeType.GetDisplayName()}");
            foreach ((object? value, MemberInfo member) in this.defaultSerializer.DeserializeMembers(node, nodeType))
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