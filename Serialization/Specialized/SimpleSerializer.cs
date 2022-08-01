using System;
using System.Linq;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    /// <summary>
    /// A (de)serializer for <see cref="string"/>, <see cref="char"/>, <see cref="bool"/>, and all the numeric types (<see cref="int"/>, <see cref="float"/>, etc).
    /// </summary>
    public class SimpleSerializer : ISerializer
    {
        private static readonly Type[] simpleTypes = {typeof(string), typeof(char), typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),};
        
        /// <summary>
        /// Serializes the simple type <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize. Its <see cref="Type"/> must be one of <see cref="string"/>, <see cref="char"/>, <see cref="bool"/>, or the numeric types(<see cref="int"/>, <see cref="float"/>, etc).</param>
        /// <param name="type">The <see cref="Type"/> to serialize <paramref name="instance"/> as, in case it is different from <paramref name="instance"/>'s <see cref="Type"/>.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/>'s <see cref="Type"/> is not a simple type.</exception>
        public XmlNode Serialize(object instance, Type? type = null)
        {
            type ??= instance.GetType();
            if (!SimpleSerializer.simpleTypes.Contains(type))
            {
                throw new SerializationException(instance, $"\"{type.GetDisplayName()}\" is not a suitable {nameof(Type)} for {typeof(SimpleSerializer).GetDisplayName()}");
            }

            XmlDocument context = new();
            XmlElement element = context.CreateElement(type.GetDisplayName());
            element.AppendChild(context.CreateTextNode(instance.ToString()));
            return element;
        }

        /// <summary>
        /// Deserializes <paramref name="node"/> into a simple type.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <param name="type">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as. It must be one of <see cref="string"/>, <see cref="char"/>, <see cref="bool"/>, or the numeric types(<see cref="int"/>, <see cref="float"/>, etc).</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="type"/> is not a simple type.</exception>
        public object Deserialize(XmlNode node, Type? type = null)
        {
            if (!node.HasChildNodes)
            {
                throw new SerializationException(node, "Node contains no textual data");
            }

            string text = node.ChildNodes[0].InnerText;

            if (type == typeof(string))
            {
                return text;
            }

            if (type == typeof(char))
            {
                return Char.Parse(text);
            }

            if (type == typeof(bool))
            {
                return Boolean.Parse(text);
            }

            if (type == typeof(sbyte))
            {
                return SByte.Parse(text);
            }
            if (type == typeof(byte))
            {
                return Byte.Parse(text);
            }

            if (type == typeof(short))
            {
                return Int16.Parse(text);
            }
            if (type == typeof(ushort))
            {
                return UInt16.Parse(text);
            }

            if (type == typeof(int))
            {
                return Int32.Parse(text);
            }
            if (type == typeof(uint))
            {
                return UInt32.Parse(text);
            }

            if (type == typeof(long))
            {
                return Int64.Parse(text);
            }
            if (type == typeof(ulong))
            {
                return UInt64.Parse(text);
            }

            if (type == typeof(float))
            {
                return Single.Parse(text);
            }
            if (type == typeof(double))
            {
                return Double.Parse(text);
            }
            if (type == typeof(decimal))
            {
                return Decimal.Parse(text);
            }

            throw new SerializationException(node, $"Unable to find simple {nameof(Type)} suitable for {typeof(SimpleSerializer).GetDisplayName()} ");
        }
    }
}