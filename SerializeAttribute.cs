using System;

namespace Godot.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SerializeAttribute : Attribute
    {
        public SerializeAttribute(bool serializable = true)
        {
            this.Serializable = serializable;
        }

        public bool Serializable
        {
            get;
        }
    }
}