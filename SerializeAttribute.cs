using System;

namespace Godot.Serialization
{
    /// <summary>
    /// Indicates whether a field or property can be (de)serialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SerializeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SerializeAttribute"/>.
        /// </summary>
        /// <param name="serializable">Indicates whether the field or property annotated with the <see cref="SerializeAttribute"/> must always be (de)serialized (<see langword="true"/>) or never be (de)serialized (<see langword="false"/>).</param>
        public SerializeAttribute(bool serializable = true)
        {
            this.Serializable = serializable;
        }

        /// <summary>
        /// Indicates whether the field or property annotated with the <see cref="SerializeAttribute"/> must always be (de)serialized (<see langword="true"/>) or never be (de)serialized (<see langword="false"/>).
        /// </summary>
        public bool Serializable
        {
            get;
        }
    }
}