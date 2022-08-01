using System;
using System.Xml;

using Godot.Utility.Extensions;

namespace Godot.Serialization
{
    /// <summary>
    /// The exception that is thrown when there is a failed attempt at serializing an <see cref="object"/> or deserializing an <see cref="XmlNode"/>.
    /// </summary>
    public class SerializationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SerializationException"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> that was being deserialized.</param>
        /// <param name="message">A brief description of the issue.</param>
        public SerializationException(XmlNode node, string message) : base($"Could not deserialize XML node {node.Name}: {message}")
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SerializationException"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> that was being deserialized.</param>
        /// <param name="cause">The inner <see cref="Exception"/> that caused the issue.</param>
        public SerializationException(XmlNode node, Exception cause) : base($"Could not deserialize XML node {node.Name}.{System.Environment.NewLine}{cause}")
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SerializationException"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> that was being serialized.</param>
        /// <param name="message">A brief description of the issue.</param>
        public SerializationException(object instance, string message) : base($"Could not serialize object of {nameof(Type)} \"{instance.GetType().GetDisplayName()}\": {message}")
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SerializationException"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> that was being serialized.</param>
        /// <param name="cause">The inner <see cref="Exception"/> that caused the issue.</param>
        public SerializationException(object instance, Exception cause) : base($"Could not serialize object of {nameof(Type)} \"{instance.GetType().GetDisplayName()}\".{System.Environment.NewLine}{cause}")
        {
        }
    }
}