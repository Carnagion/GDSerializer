using System;
using System.Xml;

namespace Godot.Serialization
{
    /// <summary>
    /// Defines methods for XML (de)serialization.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize.</param>
        /// <param name="type">The <see cref="Type"/> to serialize <paramref name="instance"/> as, in case it is different from <paramref name="instance"/>'s <see cref="Type"/>.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        XmlNode Serialize(object instance, Type? type = null);
        
        /// <summary>
        /// Deserializes <paramref name="node"/> into an <see cref="object"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <param name="type">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as, in case it is not apparent from <paramref name="node"/>'s attributes.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        object? Deserialize(XmlNode node, Type? type = null);
    }
}