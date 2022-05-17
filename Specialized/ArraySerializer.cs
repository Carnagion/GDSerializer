using System;
using System.Collections;
using System.Linq;
using System.Xml;

using Godot.Serialization.Utility.Exceptions;
using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    public class ArraySerializer : CollectionSerializer
    {
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
                arrayElement.SetAttribute("Type", arrayType.FullName);
                ArraySerializer.SerializeItems(instance, itemType).ForEach(node => arrayElement.AppendChild(context.ImportNode(node, true)));
                return arrayElement;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(instance, exception);
            }
        }

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
                foreach (object? item in ArraySerializer.DeserializeItems(node, itemType))
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