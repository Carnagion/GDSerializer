using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Godot.Serialization.Utility.Exceptions;
using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    public class EnumerableSerializer : Serializer
    {
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