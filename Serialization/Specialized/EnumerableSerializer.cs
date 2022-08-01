using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for <see cref="IEnumerable{T}"/>.
    /// </summary>
    public class EnumerableSerializer : CollectionSerializer
    {
        /// <summary>
        /// Initialises a new <see cref="EnumerableSerializer"/> with the specified parameters.
        /// </summary>
        /// <param name="itemSerializer">The serializer to use when (de)serializing the <see cref="IEnumerable{T}"/>'s items.</param>
        public EnumerableSerializer(ISerializer itemSerializer) : base(itemSerializer)
        {
        }
        
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize. Its <see cref="Type"/> must be exactly <see cref="IEnumerable{T}"/>.</param>
        /// <param name="enumerableType">The <see cref="Type"/> to serialize <paramref name="instance"/> as.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public override XmlNode Serialize(object instance, Type? enumerableType = null)
        {
            enumerableType ??= instance.GetType();
            if (!enumerableType.IsExactlyGenericType(typeof(IEnumerable<>)))
            {
                throw new SerializationException(instance, $"\"{enumerableType.GetDisplayName()}\" cannot be (de)serialized by {typeof(EnumerableSerializer).GetDisplayName()}");
            }
            
            try
            {
                Type itemType = enumerableType.GenericTypeArguments[0];
                
                XmlDocument context = new();
                XmlElement enumerableElement = context.CreateElement("Enumerable");
                enumerableElement.SetAttribute("Type", enumerableType.FullName);
                this.SerializeItems(instance, itemType).ForEach(node => enumerableElement.AppendChild(context.ImportNode(node, true)));
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
            enumerableType ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            if (!enumerableType.IsExactlyGenericType(typeof(IEnumerable<>)))
            {
                throw new SerializationException(node, $"\"{enumerableType.GetDisplayName()}\" cannot be (de)serialized by {typeof(EnumerableSerializer).GetDisplayName()}");
            }
            
            try
            {
                Type itemType = enumerableType.GenericTypeArguments[0];
                Type listType = typeof(List<>).MakeGenericType(itemType);
                object enumerable = base.Deserialize(node, listType);
                return typeof(IEnumerableExtensions).GetMethod("Copy")!.MakeGenericMethod(itemType).Invoke(null, new[] {enumerable,})!;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }
    }
}