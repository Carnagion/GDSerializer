using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

using Godot.Serialization.Specialized;
using Godot.Serialization.Utility;
using Godot.Serialization.Utility.Exceptions;
using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization
{
    /// <summary>
    /// A default <see cref="ISerializer"/> implementation that allows configurable serializers for specific <see cref="Type"/>s.
    /// </summary>
    public class Serializer : ISerializer
    {
        private const BindingFlags instanceBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>
        /// An <see cref="OrderedDictionary{TKey,TValue}"/> of specialized <see cref="ISerializer"/>s for specific <see cref="Type"/>s. These serializers will be used by the <see cref="Serializer"/> when possible.
        /// </summary>
        public static OrderedDictionary<Type, ISerializer> Specialized // Must be static; making it instance will cause a stack overflow due to it being recursively created in inheriting classes
        {
            get;
        } = new(19)
        {
            {typeof(string), new SimpleSerializer()},
            {typeof(char), new SimpleSerializer()},
            {typeof(bool), new SimpleSerializer()},
            {typeof(sbyte), new SimpleSerializer()},
            {typeof(byte), new SimpleSerializer()},
            {typeof(short), new SimpleSerializer()},
            {typeof(ushort), new SimpleSerializer()},
            {typeof(int), new SimpleSerializer()},
            {typeof(uint), new SimpleSerializer()},
            {typeof(long), new SimpleSerializer()},
            {typeof(ulong), new SimpleSerializer()},
            {typeof(float), new SimpleSerializer()},
            {typeof(double), new SimpleSerializer()},
            {typeof(decimal), new SimpleSerializer()},
            {typeof(IDictionary<,>), new DictionarySerializer()},
            {typeof(ICollection<>), new CollectionSerializer()},
            {typeof(IEnumerable<>), new EnumerableSerializer()},
            {typeof(Vector2), new VectorSerializer()},
            {typeof(Vector3), new VectorSerializer()},
        };

        /// <summary>
        /// Serializes <paramref name="instance"/> into an <see cref="XmlNode"/>.
        /// </summary>
        /// <param name="instance">The <see cref="object"/> to serialize.</param>
        /// <param name="context">The <see cref="XmlDocument"/> to use when creating new <see cref="XmlNode"/>s that will be returned as part of result.</param>
        /// <returns>An <see cref="XmlNode"/> that represents <paramref name="instance"/> and the serializable data stored in it.</returns>
        /// <exception cref="SerializationException">Thrown if <paramref name="instance"/> could not be serialized due to unexpected errors or invalid input.</exception>
        public virtual XmlNode Serialize(object instance, XmlDocument? context = null)
        {
            Type type = instance.GetType();
            
            // Use a more specialized serializer if possible
            ISerializer? serializer = Serializer.GetSpecialSerializerForType(type);
            if (serializer is not null)
            {
                return serializer.Serialize(instance, context);
            }

            try
            {
                context ??= new();
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
                foreach (PropertyInfo property in from property in type.GetProperties(Serializer.instanceBindingFlags)
                                                  where property.IsSerializable()
                                                  select property)
                {
                    object? value = property.GetValue(instance);
                    if (value is null)
                    {
                        continue;
                    }
                    XmlNode node = context.CreateElement(property.Name);
                    this.Serialize(value, context).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(child => node.AppendChild(child));
                    element.AppendChild(node);
                }
                
                // Recursively serialize fields
                foreach (FieldInfo field in from field in type.GetFields(Serializer.instanceBindingFlags)
                                            where field.IsSerializable()
                                            select field)
                {
                    object? value = field.GetValue(instance);
                    if (value is null)
                    {
                        continue;
                    }
                    XmlNode node= context.CreateElement(field.Name);
                    this.Serialize(value, context).ChildNodes
                        .Cast<XmlNode>()
                        .ForEach(child => node.AppendChild(child));
                    element.AppendChild(node);
                }

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
        public virtual object Deserialize(XmlNode node, Type? type = null)
        {
            type ??= Serializer.GetTypeToDeserialize(node) ?? throw new SerializationException(node, $"No {nameof(Type)} found to instantiate");
            
            // Use a more specialized deserializer if possible
            ISerializer? serializer = Serializer.GetSpecialSerializerForType(type);
            if (serializer is not null)
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
                    PropertyInfo? property = type.GetProperty(child.Name, Serializer.instanceBindingFlags);
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
                    FieldInfo? field = type.GetField(child.Name, Serializer.instanceBindingFlags);
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
        public T Deserialize<T>(XmlNode node)
        {
            return (T)this.Deserialize(node, typeof(T));
        }

        private static Type? GetTypeToDeserialize(XmlNode node)
        {
            string name = (node.Attributes?["Type"]?.InnerText ?? node.Name)
                .Replace("&lt;", "<")
                .Replace("&gt;", ">");
            return (from assembly in AppDomain.CurrentDomain.GetAssemblies().Distinct()
                    select assembly.GetType(name))
                .NotNull()
                .FirstOrDefault();
        }

        private static ISerializer? GetSpecialSerializerForType(Type type)
        {
            ISerializer? serializer;
            if (type.IsGenericType)
            {
                serializer = Serializer.Specialized.GetValueOrDefault(type);
                if (serializer is not null)
                {
                    return serializer;
                }
                Type? match = Serializer.Specialized.Keys
                    .FirstOrDefault(type.IsExactlyGenericType);
                if (match is not null)
                {
                    return Serializer.Specialized[match];
                }
                match = Serializer.Specialized.Keys
                    .FirstOrDefault(type.DerivesFromGenericType);
                if (match is not null)
                {
                    return Serializer.Specialized[match];
                }
            }
            else
            {
                Serializer.Specialized.TryGetValue(type, out serializer);
            }
            return serializer;
        }
    }
}