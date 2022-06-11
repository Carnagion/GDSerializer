using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for types that implement <see cref="IDictionary{TKey,TValue}"/>.
    /// </summary>
    public class DictionarySerializer : ISerializer
    {
        /// <summary>
        /// Initialises a new <see cref="DictionarySerializer"/> with the specified parameters.
        /// </summary>
        /// <param name="itemSerializer">The serializer to use when (de)serializing the <see cref="IDictionary{TKey,TValue}"/>'s items.</param>
        public DictionarySerializer(ISerializer itemSerializer)
        {
            this.itemSerializer = itemSerializer;
        }

        private readonly ISerializer itemSerializer;
        
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize. It must implement <see cref="IDictionary{TKey,TValue}"/>.</param>
        /// <param name="dictionaryType">The <see cref="Type"/> to serialize <paramref name="instance"/> as.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public XmlNode Serialize(object instance, Type? dictionaryType = null)
        {
            dictionaryType ??= instance.GetType();
            if (!dictionaryType.DerivesFromGenericType(typeof(IDictionary<,>)))
            {
                throw new SerializationException(instance, $"\"{dictionaryType.GetDisplayName()}\" cannot be (de)serialized by {typeof(DictionarySerializer).GetDisplayName()}");
            }
            
            try
            {
                Type keyType = dictionaryType.GenericTypeArguments[0];
                Type valueType = dictionaryType.GenericTypeArguments[1];
                
                Type pairType = typeof(KeyValuePair<,>).MakeGenericType(dictionaryType.GenericTypeArguments);
                
                PropertyInfo keyProperty = pairType.GetProperty("Key")!;
                PropertyInfo valueProperty = pairType.GetProperty("Value")!;
            
                XmlDocument context = new();
                XmlElement dictionaryElement = context.CreateElement("Dictionary");
                dictionaryElement.SetAttribute("Type", dictionaryType.FullName);

                foreach (object item in (IEnumerable)instance)
                {
                    object key = keyProperty.GetValue(item)!;
                    object value = valueProperty.GetValue(item)!;
                    
                    XmlElement keyElement = context.CreateElement("key");
                    if (key.GetType() != keyType)
                    {
                        keyElement.SetAttribute("Type", key.GetType().FullName);
                    }
                    this.itemSerializer.Serialize(key, key.GetType()).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(node => keyElement.AppendChild(context.ImportNode(node, true)));
                    
                    XmlElement valueElement = context.CreateElement("value");
                    if (value.GetType() != valueType)
                    {
                        valueElement.SetAttribute("Type", value.GetType().FullName);
                    }
                    this.itemSerializer.Serialize(value, value.GetType()).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(node => valueElement.AppendChild(context.ImportNode(node, true)));
                    
                    XmlElement itemElement = context.CreateElement("item");
                    itemElement.AppendChild(keyElement);
                    itemElement.AppendChild(valueElement);
                    
                    dictionaryElement.AppendChild(itemElement);
                }

                return dictionaryElement;
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
        /// <param name="dictionaryType">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as. It must implement <see cref="IDictionary{TKey,TValue}"/>.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="node"/> could not be deserialized due to unexpected errors or invalid input.</exception>
        public object Deserialize(XmlNode node, Type? dictionaryType = null)
        {
            dictionaryType ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            if (!dictionaryType.DerivesFromGenericType(typeof(IDictionary<,>)))
            {
                throw new SerializationException(node, $"\"{dictionaryType.GetDisplayName()}\" cannot be (de)serialized by {typeof(DictionarySerializer).GetDisplayName()}");
            }

            try
            {
                MethodInfo add = dictionaryType.GetMethod("Add")!;
                
                Type keyType = dictionaryType.GenericTypeArguments[0];
                Type valueType = dictionaryType.GenericTypeArguments[1];

                if (dictionaryType.IsExactlyGenericType(typeof(IDictionary<,>)))
                {
                    dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                }
                
                Serializer serializer = new();

                object dictionary = Activator.CreateInstance(dictionaryType, true) ?? throw new SerializationException(node, $"Unable to instantiate {dictionaryType.GetDisplayName()}");
                foreach (XmlNode child in from XmlNode child in node.ChildNodes
                                          where child.NodeType is XmlNodeType.Element
                                          select child)
                {
                    if (child.Name != "item")
                    {
                        throw new SerializationException(child, $"Invalid XML node (all nodes in a {typeof(Dictionary<,>).GetDisplayName()} must be named \"item\")");
                    }
                    
                    XmlNode key = child.ChildNodes
                        .Cast<XmlNode>()
                        .SingleOrDefault(grandchild => grandchild.Name == "key") ?? throw new SerializationException(child, "No key node present");
                    XmlNode value = child.ChildNodes
                        .Cast<XmlNode>()
                        .SingleOrDefault(grandchild => grandchild.Name == "value") ?? throw new SerializationException(child, "No value node present");

                    add.Invoke(dictionary, new[] {serializer.Deserialize(key, key.GetTypeToDeserialize() ?? keyType), serializer.Deserialize(value, value.GetTypeToDeserialize() ?? valueType),});
                }
                return dictionary;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }
    }
}