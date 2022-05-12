using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

using Godot.Serialization.Utility.Exceptions;
using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization.Specialized
{
    public class VectorSerializer : ISerializer
    {
        private static readonly Dictionary<Type, Regex> vectorRegexes = new(2)
        {
            {typeof(Vector2), new(@"\(\s*(?<x>[+-]?\d+(?:\.\d+)?)\s*,\s*(?<y>[+-]?\d+(?:\.\d+)?)\s*\)")},
            {typeof(Vector3), new(@"\(\s*(?<x>[+-]?\d+(?:\.\d+)?)\s*,\s*(?<y>[+-]?\d+(?:\.\d+)?)\s*,\s*(?<z>[+-]?\d+(?:\.\d+)?)\s*\)")},
        };

        public XmlNode Serialize(object instance, XmlDocument? context = null)
        {
            context ??= new();
            switch (instance)
            {
                case Vector2 vector2:
                    XmlElement element2 = context.CreateElement(typeof(Vector2).FullName);
                    element2.AppendChild(context.CreateTextNode($"({vector2.x}, {vector2.y})"));
                    return element2;
                case Vector3 vector3:
                    XmlElement element3 = context.CreateElement(typeof(Vector3).FullName);
                    element3.AppendChild(context.CreateTextNode($"({vector3.x}, {vector3.y}, {vector3.z})"));
                    return element3;
                default:
                    throw new SerializationException(instance, $"\"{instance.GetType().GetDisplayName()}\" cannot be serialized by {typeof(VectorSerializer).GetDisplayName()}");
            }
        }

        public object Deserialize(XmlNode node, Type? type = null)
        {
            if (type is null)
            {
                throw new SerializationException(node, $"No {nameof(type)} provided");
            }
            
            if (!node.HasChildNodes)
            {
                throw new SerializationException(node, "Node contains no textual data");
            }

            try
            {
                string text = node.ChildNodes[0].InnerText.Trim();
                Match match = VectorSerializer.vectorRegexes[type].Match(text);
                if (!match.Success)
                {
                    throw new SerializationException(node, "Invalid vector format");
                }

                float x = Single.Parse(match.Groups["x"].Value);
                float y = Single.Parse(match.Groups["y"].Value);
                if (type == typeof(Vector2))
                {
                    return new Vector2(x, y);
                }
                float z = Single.Parse(match.Groups["z"].Value);
                return new Vector3(x, y, z);
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }
    }
}