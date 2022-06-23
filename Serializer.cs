using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Xml;

using Godot.Serialization.Specialized;
using Godot.Serialization.Utility;
using Godot.Serialization.Utility.Extensions;

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
        /// <param name="referenceSources">An array of <see cref="XmlNode"/>s to use when deserializing <see cref="XmlNode"/>s that refer other <see cref="XmlNode"/>s through an ID.</param>
        public Serializer(params XmlNode[] referenceSources)
        {
            this.specialized = new(19)
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
                {typeof(Array), new ArraySerializer(this)},
                {typeof(IDictionary<,>), new DictionarySerializer(this)},
                {typeof(ICollection<>), new CollectionSerializer(this)},
                {typeof(IEnumerable<>), new EnumerableSerializer(this)},
                {typeof(Vector2), Serializer.vector},
                {typeof(Vector3), Serializer.vector},
                {typeof(Enum), new EnumSerializer()},
            };
            this.referenceSources = referenceSources;
            this.referenceStorage = referenceSources.Any() ? new() : null!;
        }

        /// <summary>
        /// Initialises a new <see cref="Serializer"/> with the specified parameters.
        /// </summary>
        /// <param name="specializedSerializers">The specialized serializers to use when (de)serializing specific <see cref="Type"/>s.</param>
        /// <param name="referenceSources">An array of <see cref="XmlNode"/>s to use when deserializing <see cref="XmlNode"/>s that refer other <see cref="XmlNode"/>s through an ID.</param>
        public Serializer(OrderedDictionary<Type, ISerializer> specializedSerializers, params XmlNode[] referenceSources)
        {
            this.specialized = specializedSerializers;
            this.referenceSources = referenceSources;
            this.referenceStorage = referenceSources.Any() ? new() : null!;
        }
        
        private const BindingFlags instanceBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly SimpleSerializer simple = new();

        private static readonly VectorSerializer vector = new();

        private readonly OrderedDictionary<Type, ISerializer> specialized;

        private readonly XmlNode[] referenceSources;

        private readonly Dictionary<string, object?> referenceStorage;

        /// <summary>
        /// Specialized <see cref="ISerializer"/>s for specific <see cref="Type"/>s. These serializers will be used by the <see cref="Serializer"/> when possible.
        /// </summary>
        public IReadOnlyDictionary<Type, ISerializer> Specialized
        {
            get
            {
                return this.specialized;
            }
        }

        /// <summary>
        /// An <see cref="IEnumerable{T}"/> of <see cref="XmlNode"/>s that contain <see cref="XmlNode"/>s with IDs referenced by other <see cref="XmlNode"/>s.
        /// </summary>
        public IEnumerable<XmlNode> ReferenceSources
        {
            get
            {
                return this.referenceSources;
            }
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
            
            // Use a more specialized serializer if possible
            if (this.TryGetSpecialSerializerForType(type, out ISerializer? serializer))
            {
                return serializer.Serialize(instance, type);
            }

            try
            {
                XmlDocument context = new();
                XmlElement element;
                // Use the "Type" attribute if generic or nested type as ` and + are not allowed as XML node names
                if (type.IsGenericType)
                {
                    element = context.CreateElement("Generic");
                    element.SetAttribute("Type", type.FullName);
                }
                else if (type.IsNested)
                {
                    element = context.CreateElement("Nested");
                    element.SetAttribute("Type", type.FullName);
                }
                else
                {
                    element = context.CreateElement(type.GetDisplayName());
                }

                // Recursively serialize properties
                foreach (PropertyInfo property in from property in type.GetAllMembers<PropertyInfo>(Serializer.instanceBindingFlags)
                                                  where property.IsSerializable()
                                                  select property)
                {
                    object? value = property.GetValue(instance);
                    if (value is null)
                    {
                        continue;
                    }
                    XmlNode node = context.CreateElement(property.Name);
                    this.Serialize(value, property.PropertyType).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(child => node.AppendChild(context.ImportNode(child, true)));
                    element.AppendChild(node);
                }
                
                // Recursively serialize fields
                foreach (FieldInfo field in from field in type.GetAllMembers<FieldInfo>(Serializer.instanceBindingFlags)
                                            where field.IsSerializable()
                                            select field)
                {
                    object? value = field.GetValue(instance);
                    if (value is null)
                    {
                        continue;
                    }
                    XmlNode node= context.CreateElement(field.Name);
                    this.Serialize(value, field.FieldType).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(child => node.AppendChild(context.ImportNode(child, true)));
                    element.AppendChild(node);
                }

                // Invoke all [AfterSerialization] methods
                (from method in type.GetAllMembers<MethodInfo>()
                 where method.GetCustomAttribute<AfterSerializationAttribute>() is not null
                 select method).ForEach(method => method.Invoke(method.IsStatic ? null : instance, null));

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
            if (this.TryDeserializeReferencedNode(node, out object? referenced))
            {
                return referenced;
            }
            
            // Use a more specialized deserializer if possible
            if (this.TryGetSpecialSerializerForType(type, out ISerializer? serializer))
            {
                return serializer.Deserialize(node, type);
            }

            try
            {
                HashSet<MemberInfo> deserialized = new();

                object instance = Activator.CreateInstance(type, true) ?? throw new SerializationException(node, $"Unable to instantiate {type.GetDisplayName()}");
                foreach (XmlNode child in from XmlNode child in node.ChildNodes
                                          where child.NodeType is XmlNodeType.Element
                                          select child)
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
                        property.SetValue(instance, this.Deserialize(child, property.PropertyType));
                        deserialized.Add(property);
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
                        field.SetValue(instance, this.Deserialize(child, field.FieldType));
                        deserialized.Add(field);
                        continue;
                    }

                    throw new SerializationException(child, $"{type.GetDisplayName()} has no field or property named \"{child.Name}\"");
                }

                // Ensure that properties/fields with [Serialize(true)] have been deserialized and those with [Serialize(false)] have not been deserialized
                MemberInfo[] members = type.GetMembers(Serializer.instanceBindingFlags);
                if (!deserialized.ContainsAll(from member in members 
                                              let attribute = member.GetCustomAttribute<SerializeAttribute>() 
                                              where attribute is not null && attribute.Serializable 
                                              select member))
                {
                    throw new SerializationException(node, $"One or more mandatory properties or fields of {type.GetDisplayName()} were not deserialized");
                }

                // Invoke all [AfterDeserialization] methods
                (from method in type.GetAllMembers<MethodInfo>()
                 where method.GetCustomAttribute<AfterDeserializationAttribute>() is not null
                 select method).ForEach(method => method.Invoke(method.IsStatic ? null : instance, null));
                
                // Add deserialized instance to reference storage if it has an ID
                string? id = node.Attributes?["Id"]?.InnerText;
                if (id is not null)
                {
                    this.referenceStorage.Add(id, instance);
                }
                
                return instance;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }

        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize.</param>
        /// <typeparam name="T">The <see cref="Type"/> to serialize <paramref name="instance"/> as.</typeparam>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        public XmlNode Serialize<T>(T instance) where T : notnull
        {
            return this.Serialize(instance, typeof(T));
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
            if (!this.ReferenceSources.Any())
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
            XmlNode referencedNode = (from source in this.ReferenceSources
                                      select source.SelectSingleNode($"*[@Id='{referencedId}']")).FirstOrDefault() ?? throw new SerializationException(node, $"Referenced XML node with ID \"{referencedId}\" not found");
            instance = this.Deserialize(referencedNode);
            return true;
        }
    }
}