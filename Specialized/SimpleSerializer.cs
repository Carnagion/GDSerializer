using System;
using System.Linq;
using System.Xml;

using Godot.Serialization.Utility.Exceptions;
using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    public class SimpleSerializer : ISerializer
    {
        private static readonly Type[] simpleTypes = {typeof(string), typeof(char), typeof(bool), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),};
        
        public XmlNode Serialize(object instance, XmlDocument? context = null)
        {
            Type type = instance.GetType();
            if (!SimpleSerializer.simpleTypes.Contains(type))
            {
                throw new SerializationException(instance, $"\"{type.GetDisplayName()}\" is not a suitable {nameof(Type)} for {typeof(SimpleSerializer).GetDisplayName()}");
            }

            context ??= new();
            XmlElement element = context.CreateElement(type.GetDisplayName()!);
            element.AppendChild(context.CreateTextNode(instance.ToString()));
            return element;
        }

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