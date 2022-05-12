using System;
using System.Xml;

using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization.Utility.Exceptions
{
    public class SerializationException : Exception
    {
        public SerializationException(XmlNode node, string message) : base($"Could not deserialize XML node {node.Name}: {message}")
        {
        }

        public SerializationException(XmlNode node, Exception cause) : base($"Could not deserialize XML node {node.Name}.{System.Environment.NewLine}{cause}")
        {
        }

        public SerializationException(object instance, string message) : base($"Could not serialize object of {nameof(Type)} \"{instance.GetType().GetDisplayName()}\": {message}")
        {
        }

        public SerializationException(object instance, Exception cause) : base($"Could not serialize object of {nameof(Type)} \"{instance.GetType().GetDisplayName()}\".{System.Environment.NewLine}{cause}")
        {
        }
    }
}