using System;
using System.Xml;

namespace Godot.Serialization
{
    public interface ISerializer
    {
        XmlNode Serialize(object instance, XmlDocument? context = null);
        
        object Deserialize(XmlNode node, Type? type = null);
    }
}