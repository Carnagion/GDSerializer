using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

using Godot.Serialization.Utility.Exceptions;
using Godot.Serialization.Utility.Extensions;

namespace Godot.Serialization
{
    public class Serializer : ISerializer
    {
        private const BindingFlags instanceBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static Dictionary<Type, ISerializer> Specialized
        {
            get;
        } = new();

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
                        property.SetValue(instance, this.Deserialize(child, property.PropertyType));
                        deserialized.Add(property);
                        continue;
                    }
                
                    // Recursively deserialize field
                    FieldInfo? field = type.GetField(child.Name, Serializer.instanceBindingFlags);
                    if (field is not null)
                    {
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
                    throw new SerializationException(node, $"One or more mandatory values of {type.GetDisplayName()} were not deserialized");
                }
                if (deserialized.ContainsAny(from member in members 
                                             let attribute = member.GetCustomAttribute<SerializeAttribute>() 
                                             where attribute is not null && !attribute.Serializable 
                                             select member))
                {
                    throw new SerializationException(node, $"One or more non-(de)serializable values of {type.GetDisplayName()} were deserialized");
                }

                return instance;
            }
            catch (Exception exception) when (exception is not SerializationException)
            {
                throw new SerializationException(node, exception);
            }
        }

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