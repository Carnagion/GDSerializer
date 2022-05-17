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
    /// <summary>
    /// A (de)serializer for types that implement <see cref="ICollection{T}"/>.
    /// </summary>
    public class CollectionSerializer : ISerializer
    {
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize. It must implement <see cref="ICollection{T}"/>.</param>
        /// <param name="collectionType">The <see cref="Type"/> to serialize <paramref name="instance"/> as.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public virtual XmlNode Serialize(object instance, Type? collectionType = null)
        {
            collectionType ??= instance.GetType();
            if (!collectionType.DerivesFromGenericType(typeof(ICollection<>)))
            {
                throw new SerializationException(instance, $"\"{collectionType.GetDisplayName()}\" cannot be (de)serialized by {typeof(CollectionSerializer).GetDisplayName()}");
            }
            
            try
            {
                Type itemType = collectionType.GenericTypeArguments[0];
                
                XmlDocument context = new();
                XmlElement collectionElement = context.CreateElement("Collection");
                collectionElement.SetAttribute("Type", collectionType.FullName);

                Serializer serializer = new();
                
                foreach (object item in (IEnumerable)instance)
                {
                    XmlElement itemElement = context.CreateElement("item");
                    if (item.GetType() != itemType)
                    {
                        itemElement.SetAttribute("Type", item.GetType().FullName);
                    }
                    serializer.Serialize(item, item.GetType()).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(node => itemElement.AppendChild(node));
                    collectionElement.AppendChild(context.ImportNode(itemElement, true));
                }
                return collectionElement;
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
        /// <param name="collectionType">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as. It must implement <see cref="ICollection{T}"/>.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="node"/> could not be deserialized due to unexpected errors or invalid input.</exception>
        public virtual object Deserialize(XmlNode node, Type? collectionType = null)
        {
            collectionType ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
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

                Serializer serializer = new();
            
                object collection = Activator.CreateInstance(collectionType, true) ?? throw new SerializationException(node, $"Unable to instantiate {collectionType.GetDisplayName()}");
                foreach (XmlNode child in from XmlNode child in node.ChildNodes
                                          where child.NodeType is XmlNodeType.Element
                                          select child)
                {
                    if (child.Name != "item")
                    {
                        throw new SerializationException(child, "Invalid XML node (all nodes in a collection must be named \"item\")");
                    }
                    add.Invoke(collection, new[] {serializer.Deserialize(child, child.GetTypeToDeserialize() ?? itemType),});
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