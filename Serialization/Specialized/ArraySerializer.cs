using System;
using System.Collections;
using System.Linq;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for arrays.
    /// </summary>
    public class ArraySerializer : CollectionSerializer
    {
        /// <summary>
        /// Initialises a new <see cref="ArraySerializer"/> with the specified parameters.
        /// </summary>
        /// <param name="itemSerializer">The serializer to use when (de)serializing the array's items.</param>
        public ArraySerializer(ISerializer itemSerializer) : base(itemSerializer)
        {
        }
        
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize. It must be an array.</param>
        /// <param name="arrayType">The <see cref="Type"/> to serialize <paramref name="instance"/> as. It must be an array type.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public override XmlNode Serialize(object instance, Type? arrayType = null)
        {
            arrayType ??= instance.GetType();
            if (!arrayType.IsArray)
            {
                throw new SerializationException(instance, $"\"{arrayType.GetDisplayName()}\" cannot be serialized by {typeof(ArraySerializer).GetDisplayName()}");
            }
            
            try
            {
                Type itemType = arrayType.GetElementType()!;
                
                XmlDocument context = new();
                XmlElement arrayElement = context.CreateElement("Array");
                arrayElement.SetAttribute("Type", $"{itemType.FullName}[]");
                this.SerializeItems(instance, itemType).ForEach(node => arrayElement.AppendChild(context.ImportNode(node, true)));
                return arrayElement;
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
        /// <param name="arrayType">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as. It must be an array type.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if a <see cref="Type"/> could not be inferred from <paramref name="node"/> or was invalid, an instance of the <see cref="Type"/> could not be created, <paramref name="node"/> contained invalid properties/fields, or <paramref name="node"/> could not be deserialized due to unexpected errors or invalid data.</exception>
        public override object Deserialize(XmlNode node, Type? arrayType = null)
        {
            arrayType ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            if (!arrayType.IsArray)
            {
                throw new SerializationException(node, $"\"{arrayType.GetDisplayName()}\" cannot be deserialized by {typeof(ArraySerializer).GetDisplayName()}");
            }
            
            try
            {
                Type itemType = arrayType.GetElementType()!;
                
                IList array = Array.CreateInstance(itemType, node.ChildNodes.Count);
                int index = 0;
                foreach (object? item in this.DeserializeItems(node, itemType))
                {
                    array[index] = item;
                    index += 1;
                }
                return array;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }
    }
}