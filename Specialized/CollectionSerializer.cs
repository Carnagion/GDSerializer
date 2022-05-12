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
    public class CollectionSerializer : Serializer
    {
        public override XmlNode Serialize(object instance, XmlDocument? context = null)
        {
            Type collectionType = instance.GetType();
            if (!collectionType.DerivesFromGenericType(typeof(ICollection<>)))
            {
                throw new SerializationException(instance, $"\"{collectionType.GetDisplayName()}\" cannot be (de)serialized by {typeof(CollectionSerializer).GetDisplayName()}");
            }

            try
            {
                context ??= new();
                XmlElement collectionElement = context.CreateElement("Collection");
                collectionElement.SetAttribute("Type", collectionType.FullName);
                foreach (object item in (IEnumerable)instance)
                {
                    XmlElement itemElement = context.CreateElement("item");
                    base.Serialize(item, context).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(node => itemElement.AppendChild(node));
                    collectionElement.AppendChild(itemElement);
                }
                return collectionElement;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(instance, exception);
            }
        }

        public override object Deserialize(XmlNode node, Type? collectionType = null)
        {
            if (collectionType is null)
            {
                throw new SerializationException(node, $"{nameof(Type)} not provided");
            }

            if (!collectionType.DerivesFromGenericType(typeof(ICollection<>)))
            {
                throw new SerializationException(node, $"\"{collectionType.GetDisplayName()}\" cannot be (de)serialized by {typeof(CollectionSerializer).GetDisplayName()}");
            }

            try
            {
                MethodInfo add = collectionType.GetMethod("Add")!;
                Type itemType = collectionType.GenericTypeArguments[0];

                if (collectionType.IsExactlyGenericType(typeof(ICollection<>)))
                {
                    collectionType = typeof(List<>).MakeGenericType(itemType);
                }
            
                object collection = Activator.CreateInstance(collectionType, true) ?? throw new SerializationException(node, $"Unable to instantiate {collectionType.GetDisplayName()}");
                foreach (XmlNode child in from XmlNode child in node.ChildNodes
                                          where child.NodeType is XmlNodeType.Element
                                          select child)
                {
                    if (child.Name != "item")
                    {
                        throw new SerializationException(child, "Invalid XML node (all nodes in a collection must be named \"item\")");
                    }
                    add.Invoke(collection, new[] {base.Deserialize(child, itemType),});
                }
                return collection;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }
    }
}