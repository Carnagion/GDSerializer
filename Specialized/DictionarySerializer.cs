using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

using Godot.Serialization.Utility.Exceptions;
using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    public class DictionarySerializer : Serializer
    {
        public override XmlNode Serialize(object instance, XmlDocument? context = null)
        {
            Type dictionaryType = instance.GetType();
            if (!dictionaryType.DerivesFromGenericType(typeof(IDictionary<,>)))
            {
                throw new SerializationException(instance, $"\"{dictionaryType.GetDisplayName()}\" cannot be (de)serialized by {typeof(DictionarySerializer).GetDisplayName()}");
            }

            try
            {
                Type pairType = typeof(KeyValuePair<,>).MakeGenericType(dictionaryType.GenericTypeArguments);
                PropertyInfo keyProperty = pairType.GetProperty("Key")!;
                PropertyInfo valueProperty = pairType.GetProperty("Value")!;
            
                context ??= new();
                XmlElement dictionaryElement = context.CreateElement("Dictionary");
                dictionaryElement.SetAttribute("Type", dictionaryType.FullName);
                foreach (object item in (IEnumerable)instance)
                {
                    XmlElement itemElement = context.CreateElement("item");
                    XmlElement keyElement = context.CreateElement("key");
                    XmlElement valueElement = context.CreateElement("value");
                    object key = keyProperty.GetValue(item)!;
                    object value = valueProperty.GetValue(item)!;
                    base.Serialize(key, context).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(node => keyElement.AppendChild(node));
                    base.Serialize(value, context).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(node => valueElement.AppendChild(node));
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

        public override object Deserialize(XmlNode node, Type? dictionaryType = null)
        {
            if (dictionaryType is null)
            {
                throw new SerializationException(node, $"{nameof(Type)} not provided");
            }

            if (!dictionaryType.DerivesFromGenericType(typeof(IDictionary<,>)))
            {
                throw new SerializationException(node, $"\"{dictionaryType.GetDisplayName()}\" cannot be (de)serialized by {typeof(CollectionSerializer).GetDisplayName()}");
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
                        .FirstOrDefault(grandchild => grandchild.Name == "key") ?? throw new SerializationException(child, "No key node present");
                    XmlNode value = child.ChildNodes
                        .Cast<XmlNode>()
                        .FirstOrDefault(grandchild => grandchild.Name == "value") ?? throw new SerializationException(child, "No value node present");

                    add.Invoke(dictionary, new[] {base.Deserialize(key, keyType), base.Deserialize(value, valueType),});
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