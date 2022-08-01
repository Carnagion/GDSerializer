using System;

namespace Godot.Serialization
{
    /// <summary>
    /// Specifies that a method is to be invoked immediately after the deserialization of the object instance it is associated with.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AfterDeserializationAttribute : Attribute
    {
    }
}