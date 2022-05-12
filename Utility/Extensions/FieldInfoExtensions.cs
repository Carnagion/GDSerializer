using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Godot.Serialization.Utility.Extensions
{
    /// <summary>
    ///  Contains extension methods for <see cref="FieldInfo"/>.
    /// </summary>
    public static class FieldInfoExtensions
    {
        private static readonly Type[] forbiddenTypes = {typeof(Pointer), typeof(IntPtr),};
        
        /// <summary>
        /// Determines if <paramref name="field"/> is a (de)serializable field.
        /// </summary>
        /// <param name="field">The <see cref="FieldInfo"/> to check.</param>
        /// <returns><see langword="true"/> if <paramref name="field"/> can be (de)serialized by an <see cref="ISerializer"/>.</returns>
        public static bool IsSerializable(this FieldInfo field)
        {
            if (field.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
            {
                return false;
            }
            if (field.FieldType.IsPointer || FieldInfoExtensions.forbiddenTypes.Contains(field.FieldType))
            {
                return false;
            }
            return field.GetCustomAttribute<SerializeAttribute>()?.Serializable ?? true;
        }
    }
}