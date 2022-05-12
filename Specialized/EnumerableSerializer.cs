using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Godot.Serialization.Utility.Exceptions;
using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for <see cref="IEnumerable{T}"/>.
    /// </summary>
    public class EnumerableSerializer : Serializer
    {
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize. Its <see cref="Type"/> must be exactly <see cref="IEnumerable{T}"/>.</param>
        /// <param name="context">The <see cref="XmlDocument"/> to use when creating new <see cref="XmlNode"/>s that will be returned as part of result.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public override XmlNode Serialize(object instance, XmlDocument? context = null)
        {
            Type enumerableType = instance.GetType();
            if (!enumerableType.IsExactlyGenericType(typeof(IEnumerable<>)))
            {
                throw new SerializationException(instance, $"\"{enumerableType.GetDisplayName()}\" cannot be (de)serialized by {typeof(EnumerableSerializer).GetDisplayName()}");
            }

            try
            {
                context ??= new();
                XmlElement enumerableElement = context.CreateElement("Enumerable");
                enumerableElement.SetAttribute("Type", enumerableType.FullName);
                foreach (object item in (IEnumerable)instance)
                {
                    XmlElement itemElement = context.CreateElement("item");
                    base.Serialize(item, context).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(node => itemElement.AppendChild(node));
                    enumerableElement.AppendChild(itemElement);
                }
                return enumerableElement;
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
        /// <param name="enumerableType">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as. It must be exactly <see cref="IEnumerable{T}"/>.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="node"/> could not be deserialized due to unexpected errors or invalid input.</exception>
        public override object Deserialize(XmlNode node, Type? enumerableType = null)
        {
            if (enumerableType is null)
            {
                throw new SerializationException(node, $"{nameof(Type)} not provided");
            }

            if (!enumerableType.IsExactlyGenericType(typeof(IEnumerable<>)))
            {
                throw new SerializationException(node, $"\"{enumerableType.GetDisplayName()}\" cannot be (de)serialized by {typeof(EnumerableSerializer).GetDisplayName()}");
            }

            try
            {
                Type itemType = enumerableType.GenericTypeArguments[0];
                Type listType = typeof(List<>).MakeGenericType(itemType);
                object enumerable = new CollectionSerializer().Deserialize(node, listType); // Cannot use base.Deserialize() here as List<T> implements both ICollection<T> and IEnumerable<T>, and Serializer's dictionary is not sorted so IEnumerable<T> may be picked leading to infinite recursive calls
                return typeof(IEnumerableExtensions).GetMethod("Copy")!.MakeGenericMethod(itemType).Invoke(null, new[] {enumerable,})!;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }
    }
}