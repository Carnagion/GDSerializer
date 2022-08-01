using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for types that implement <see cref="ICollection{T}"/>.
    /// </summary>
    public class CollectionSerializer : ISerializer
    {
        /// <summary>
        /// Initialises a new <see cref="CollectionSerializer"/> with the specified parameters.
        /// </summary>
        /// <param name="itemSerializer">The serializer to use when (de)serializing the <see cref="ICollection{T}"/>'s items.</param>
        public CollectionSerializer(ISerializer itemSerializer)
        {
            this.itemSerializer = itemSerializer;
        }

        private readonly ISerializer itemSerializer;
        
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
                this.SerializeItems(instance, itemType).ForEach(node => collectionElement.AppendChild(context.ImportNode(node, true)));
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
            
                object collection = Activator.CreateInstance(collectionType, true) ?? throw new SerializationException(node, $"Unable to instantiate {collectionType.GetDisplayName()}");
                this.DeserializeItems(node, itemType).ForEach(item => add.Invoke(collection, new[] {item,}));
                return collection;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }

        /// <summary>
        /// Serializes all items in the collection <paramref name="instance"/>. It must implement <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="instance">The collection to serialize.</param>
        /// <param name="itemType">The <see cref="Type"/> of items in <paramref name="instance"/>.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of the serialized versions of the items in <paramref name="instance"/>.</returns>
        protected IEnumerable<XmlNode> SerializeItems(object instance, Type itemType)
        {
            XmlDocument context = new();
            foreach (object item in (IEnumerable)instance)
            {
                XmlElement itemElement = context.CreateElement("item");
                if (item.GetType() != itemType)
                {
                    itemElement.SetAttribute("Type", item.GetType().FullName);
                }
                this.itemSerializer.Serialize(item, item.GetType()).ChildNodes
                    .Cast<XmlNode>()
                    .ForEach(node => itemElement.AppendChild(context.ImportNode(node, true)));
                yield return itemElement;
            }
        }

        /// <summary>
        /// Deserializes the children of <paramref name="node"/> as items of an <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> whose children are to be deserialized.</param>
        /// <param name="itemType">The <see cref="Type"/> of items in the collection.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of the deserialized children nodes of <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if one of the child nodes in <paramref name="node"/> is not named "item".</exception>
        protected IEnumerable<object?> DeserializeItems(XmlNode node, Type itemType)
        {
            return from child in node.ChildNodes.Cast<XmlNode>()
                   where child.NodeType is XmlNodeType.Element
                   select child.Name is "item" ? this.itemSerializer.Deserialize(child, child.GetTypeToDeserialize() ?? itemType) : throw new SerializationException(child, "Invalid XML node (all nodes in a collection must be named \"item\")");
        }
    }
}