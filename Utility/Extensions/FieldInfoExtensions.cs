using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Godot.Serialization.Utility.Extensions
{
    public static class FieldInfoExtensions
    {
        private static readonly Type[] forbiddenTypes = {typeof(Pointer), typeof(IntPtr),};
        
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