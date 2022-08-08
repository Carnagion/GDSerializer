using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;

using Godot.Serialization.Specialized;
using Godot.Utility;
using Godot.Utility.Extensions;

namespace Godot.Serialization
{
    /// <summary>
    /// A default <see cref="ISerializer"/> implementation that allows configurable serializers for specific <see cref="Type"/>s.
    /// </summary>
    public class Serializer : ISerializer
    {
        /// <summary>
        /// Initialises a new <see cref="Serializer"/> with the default specialized serializers.
        /// </summary>
        /// <param name="referenceSources">An <see cref="IEnumerable{T}"/> of <see cref="XmlNode"/>s to use when deserializing <see cref="XmlNode"/>s that refer other <see cref="XmlNode"/>s through an ID.</param>
        public Serializer(IEnumerable<XmlNode>? referenceSources = null)
        {
            this.Specialized = new(24)
            {
                {typeof(string), Serializer.simple},
                {typeof(char), Serializer.simple},
                {typeof(bool), Serializer.simple},
                {typeof(sbyte), Serializer.simple},
                {typeof(byte), Serializer.simple},
                {typeof(short), Serializer.simple},
                {typeof(ushort), Serializer.simple},
                {typeof(int), Serializer.simple},
                {typeof(uint), Serializer.simple},
                {typeof(long), Serializer.simple},
                {typeof(ulong), Serializer.simple},
                {typeof(float), Serializer.simple},
                {typeof(double), Serializer.simple},
                {typeof(decimal), Serializer.simple},
                {typeof(Node), new NodeSerializer(this)},
                {typeof(Array), new ArraySerializer(this)},
                {typeof(IDictionary<,>), new DictionarySerializer(this)},
                {typeof(ICollection<>), new CollectionSerializer(this)},
                {typeof(IEnumerable<>), new EnumerableSerializer(this)},
                {typeof(Vector2), Serializer.vector},
                {typeof(Vector3), Serializer.vector},
                {typeof(Enum), new EnumSerializer()},
                {typeof(Type), new TypeSerializer()},
                {typeof(XmlNode), new XmlNodeSerializer()},
            };
            this.ReferenceSources = referenceSources?.ToHashSet();
            this.referenceStorage = this.ReferenceSources is null ? null : new();
        }
        
        /// <summary>
        /// Initialises a new <see cref="Serializer"/> with the specified parameters.
        /// </summary>
        /// <param name="specializedSerializers">The specialized serializers to use when (de)serializing specific <see cref="Type"/>s.</param>
        /// <param name="referenceSources">An <see cref="IEnumerable{T}"/> of <see cref="XmlNode"/>s to use when deserializing <see cref="XmlNode"/>s that refer other <see cref="XmlNode"/>s through an ID.</param>
        public Serializer(OrderedDictionary<Type, ISerializer> specializedSerializers, IEnumerable<XmlNode>? referenceSources = null)
        {
            this.Specialized = specializedSerializers;
            this.ReferenceSources = referenceSources?.ToHashSet();
            this.referenceStorage = this.ReferenceSources is null ? null : new();
        }
        
        private const BindingFlags instanceBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        
        private static readonly Type[] forbiddenTypes = {typeof(Pointer), typeof(IntPtr),};
        
        private static readonly SimpleSerializer simple = new();
        
        private static readonly VectorSerializer vector = new();
        
        private readonly Dictionary<string, object?>? referenceStorage;
        
        /// <summary>
        /// Specialized <see cref="ISerializer"/>s for specific <see cref="Type"/>s. These serializers will be used by the <see cref="Serializer"/> when possible.
        /// </summary>
        public OrderedDictionary<Type, ISerializer> Specialized
        {
            get;
        }
        
        /// <summary>
        /// A <see cref="HashSet{T}"/> of <see cref="XmlNode"/>s that contain <see cref="XmlNode"/>s with IDs referenced by other <see cref="XmlNode"/>s.
        /// </summary>
        public HashSet<XmlNode>? ReferenceSources
        {
            get;
        }
        
        /// <summary>
        /// Determines if <paramref name="property"/> is a (de)serializable property.
        /// </summary>
        /// <param name="property">The <see cref="PropertyInfo"/> to check.</param>
        /// <returns><see langword="true"/> if <paramref name="property"/> can be (de)serialized by a <see cref="Serializer"/>, else <see langword="false"/>.</returns>
        public static bool CanSerialize(PropertyInfo property)
        {
            return property.CanRead
                && property.CanWrite
                && !property.GetIndexParameters().Any()
                && property.GetCustomAttribute<CompilerGeneratedAttribute>() is null
                && property.GetMethod.GetCustomAttribute<CompilerGeneratedAttribute>() is null
                && !Serializer.forbiddenTypes.Contains(property.PropertyType)
                && (property.GetCustomAttribute<SerializeAttribute>()?.Serializable ?? true);
        }
        
        /// <summary>
        /// Determines if <paramref name="field"/> is a (de)serializable field.
        /// </summary>
        /// <param name="field">The <see cref="FieldInfo"/> to check.</param>
        /// <returns><see langword="true"/> if <paramref name="field"/> can be (de)serialized by a <see cref="Serializer"/>, else <see langword="false"/>.</returns>
        public static bool CanSerialize(FieldInfo field)
        {
            return field.GetCustomAttribute<CompilerGeneratedAttribute>() is null 
                && !field.FieldType.IsPointer
                && !Serializer.forbiddenTypes.Contains(field.FieldType)
                && (field.GetCustomAttribute<SerializeAttribute>()?.Serializable ?? true);
        }
        
        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize.</param>
        /// <param name="type">The <see cref="Type"/> to serialize <paramref name="instance"/> as, in case it is different from <paramref name="instance"/>'s <see cref="Type"/>.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public XmlNode Serialize(object instance, Type? type = null)
        {
            type ??= instance.GetType();
            
            XmlNode element;
            
            try
            {
                // Use a more specialized serializer if possible
                if (this.TryGetSpecialSerializerForType(type, out ISerializer? serializer))
                {
                    element = serializer.Serialize(instance, type);
                }
                else
                {
                    XmlDocument context = new();
                
                    // Use the "Type" attribute if generic or nested type as ` and + are not allowed as XML node names
                    if (type.IsGenericType)
                    {
                        element = context.CreateElement("Generic");
                        ((XmlElement)element).SetAttribute("Type", type.GetDisplayName().XMLEscape());
                    }
                    else if (type.IsNested)
                    {
                        element = context.CreateElement("Nested");
                        ((XmlElement)element).SetAttribute("Type", type.GetDisplayName().XMLEscape());
                    }
                    else
                    {
                        element = context.CreateElement(type.GetDisplayName());
                    }
                
                    this.SerializeMembers(instance, type).ForEach(pair => element.AppendChild(context.ImportNode(pair.Item1, true)));
                }
                
                // Invoke all [AfterSerialization] methods
                type.GetAllMembers<MethodInfo>()
                    .Where(method => method.GetCustomAttribute<AfterSerializationAttribute>() is not null)
                    .ForEach(method => method.Invoke(method.IsStatic ? null : instance, null));
                
                return element;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(instance, exception);
            }
        }
        
        /// <summary>
        /// Deserializes <paramref name="node"/> into an <see cref="object"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <param name="type">The <see cref="Type"/> of <see cref="object"/> to deserialize the node as, in case it is not apparent from <paramref name="node"/>'s attributes.</param>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        /// <exception cref="SerializationException">Thrown if a <see cref="Type"/> could not be inferred from <paramref name="node"/> or was invalid, an instance of the <see cref="Type"/> could not be created, <paramref name="node"/> contained invalid properties/fields, or <paramref name="node"/> could not be deserialized due to unexpected errors or invalid data.</exception>
        public object? Deserialize(XmlNode node, Type? type = null)
        {
            if (node.Attributes?["Null"]?.InnerText is "True")
            {
                return null;
            }
            
            type ??= node.GetTypeToDeserialize() ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            
            // Use a previously deserialized node if referenced
            if (this.TryDeserializeReferencedNode(node, out object? instance))
            {
                return instance;
            }
            
            try
            {
                // Use a more specialized deserializer if possible
                if (this.TryGetSpecialSerializerForType(type, out ISerializer? serializer))
                {
                    instance = serializer.Deserialize(node, type);
                }
                else
                {
                    // Recursively deserialize and set members
                    instance = Activator.CreateInstance(type, true) ?? throw new SerializationException(node, $"Unable to instantiate {type.GetDisplayName()}");
                    foreach ((object? value, MemberInfo member) in this.DeserializeMembers(node, type))
                    {
                        switch (member)
                        {
                            case PropertyInfo property:
                                property.SetValue(instance, value);
                                break;
                            case FieldInfo field:
                                field.SetValue(instance, value);
                                break;
                        }
                    }
                }
                
                // Invoke all [AfterDeserialization] methods
                type.GetAllMembers<MethodInfo>()
                    .Where(method => method.GetCustomAttribute<AfterDeserializationAttribute>() is not null)
                    .ForEach(method => method.Invoke(method.IsStatic ? null : instance, null));
                
                // Add deserialized instance to reference storage if it has an ID
                string? id = node.Attributes?["Id"]?.InnerText;
                if (id is not null)
                {
                    this.referenceStorage?.Add(id, instance);
                }
                
                return instance;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }
        
        /// <summary>
        /// Deserializes <paramref name="node"/> into an <see cref="object"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to deserialize.</param>
        /// <typeparam name="T">The <see cref="Type"/> of <see cref="object"/> to deserialize <paramref name="node"/> as.</typeparam>
        /// <returns>An <see cref="object"/> that represents the serialized data stored in <paramref name="node"/>.</returns>
        public T? Deserialize<T>(XmlNode node)
        {
            return (T?)this.Deserialize(node, typeof(T));
        }
        
        /// <summary>
        /// Serializes all data members (fields and properties) of <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> whose members are to be serialized.</param>
        /// <param name="type">The <see cref="Type"/> to use when getting members of <paramref name="instance"/>, in case it is different from <paramref name="instance"/>'s <see cref="Type"/>.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of the members of <paramref name="instance"/> and their serialized values.</returns>
        public IEnumerable<(XmlNode, MemberInfo)> SerializeMembers(object? instance, Type type)
        {
            XmlDocument context = new();
            
            // Recursively serialize properties
            foreach (PropertyInfo property in type.GetAllMembers<PropertyInfo>(Serializer.instanceBindingFlags).Where(Serializer.CanSerialize))
            {
                object? value = property.GetValue(instance);
                if (value is null)
                {
                    continue;
                }
                XmlNode node = context.CreateElement(property.Name);
                this.Serialize(value, value.GetType()).ChildNodes
                    .Cast<XmlNode>()
                    .ForEach(child => node.AppendChild(context.ImportNode(child, true)));
                yield return (node, property);
            }
            
            // Recursively serialize fields
            foreach (FieldInfo field in type.GetAllMembers<FieldInfo>(Serializer.instanceBindingFlags).Where(Serializer.CanSerialize))
            {
                object? value = field.GetValue(instance);
                if (value is null)
                {
                    continue;
                }
                XmlNode node= context.CreateElement(field.Name);
                this.Serialize(value, value.GetType()).ChildNodes
                    .Cast<XmlNode>()
                    .ForEach(child => node.AppendChild(context.ImportNode(child, true)));
                yield return (node, field);
            }
        }
        
        /// <summary>
        /// Deserializes <paramref name="node"/>'s children as the data members (fields and properties) of a <see cref="Type"/>.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> whose children are to be deserialized.</param>
        /// <param name="type">The <see cref="Type"/> to use when deserializing members from <paramref name="node"/>, in case it is not apparent from <paramref name="node"/>'s attributes.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of the members of the <see cref="Type"/> and their deserialized values.</returns>
        /// <exception cref="SerializationException">Thrown if one or more mandatory members of the <see cref="Type"/> were not deserialized, or if there is another unexpected error during the process.</exception>
        public IEnumerable<(object?, MemberInfo)> DeserializeMembers(XmlNode node, Type type)
        {
            HashSet<(object?, MemberInfo)> deserialized = new();
            foreach (XmlNode child in node.ChildNodes.Cast<XmlNode>().Where(child => child.NodeType is XmlNodeType.Element))
            {
                // Recursively deserialize property
                PropertyInfo? property = type.FindProperty(child.Name, Serializer.instanceBindingFlags);
                if (property is not null)
                {
                    if (!property.CanWrite)
                    {
                        throw new SerializationException(child, $"{type.GetDisplayName()}.{property.Name} has no 'set' accessor");
                    }
                    if (!property.GetCustomAttribute<SerializeAttribute>()?.Serializable ?? false)
                    {
                        throw new SerializationException(child, $"Attempted to deserialize non-deserializable property {property.Name} in {type.GetDisplayName()}");
                    }
                    deserialized.Add((this.Deserialize(child, child.GetTypeToDeserialize() ?? property.PropertyType), property));
                    continue;
                }
                
                // Recursively deserialize field
                FieldInfo? field = type.FindField(child.Name, Serializer.instanceBindingFlags);
                if (field is not null)
                {
                    if (!field.GetCustomAttribute<SerializeAttribute>()?.Serializable ?? false)
                    {
                        throw new SerializationException(child, $"Attempted to deserialize non-deserializable field {field.Name} in {type.GetDisplayName()}");
                    }
                    deserialized.Add((this.Deserialize(child, child.GetTypeToDeserialize() ?? field.FieldType), field));
                    continue;
                }
                
                throw new SerializationException(child, $"{type.GetDisplayName()} has no field or property named \"{child.Name}\"");
            }
            
            // Ensure that properties/fields with [Serialize(true)] have been deserialized and those with [Serialize(false)] have not been deserialized
            MemberInfo[] toDeserialize = type.GetMembers(Serializer.instanceBindingFlags)
                .Select(member => (member, member.GetCustomAttribute<SerializeAttribute>()))
                .Where(pair => pair.Item2 is not null && pair.Item2.Serializable)
                .Select(pair => pair.member)
                .ToArray();
            HashSet<MemberInfo> deserializedMembers = deserialized.Select(pair => pair.Item2).ToHashSet();
            if (toDeserialize.Any() && !toDeserialize.All(deserializedMembers.Contains))
            {
                throw new SerializationException(node, $"One or more mandatory properties or fields of {type.GetDisplayName()} were not deserialized");
            }
            
            return deserialized;
        }
        
        private bool TryGetSpecialSerializerForType(Type type, [NotNullWhen(true)] out ISerializer? serializer)
        {
            if (this.Specialized.TryGetValue(type, out serializer))
            {
                return true;
            }
            if (type.IsGenericType)
            {
                Type? match = this.Specialized.Keys.FirstOrDefault(type.IsExactlyGenericType);
                match ??= this.Specialized.Keys.FirstOrDefault(type.DerivesFromGenericType);
                if (match is null)
                {
                    return false;
                }
                serializer = this.Specialized[match];
            }
            else
            {
                Type? match = this.Specialized.Keys.FirstOrDefault(key => key.IsAssignableFrom(type));
                if (match is null)
                {
                    return false;
                }
                serializer = this.Specialized[match];
            }
            return true;
        }
        
        private bool TryDeserializeReferencedNode(XmlNode node, out object? instance)
        {
            instance = null;
            if (this.referenceStorage is null || this.ReferenceSources is null)
            {
                return false;
            }
            string? referencedId = node.Attributes?["Refer"]?.InnerText;
            if (referencedId is null)
            {
                return false;
            }
            if (this.referenceStorage.TryGetValue(referencedId, out instance))
            {
                return true;
            }
            XmlNode referencedNode = this.ReferenceSources
                .Select(source => source.SelectSingleNode($"*[@Id='{referencedId}']"))
                .FirstOrDefault() ?? throw new SerializationException(node, $"Referenced XML node with ID \"{referencedId}\" not found");
            instance = this.Deserialize(referencedNode);
            return true;
        }
    }
}