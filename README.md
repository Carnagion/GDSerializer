# GDSerializer

**GDSerializer** is an XML serialization (and deserialization) library developed with Godot's C# API in mind.

It supports (de)serialization of almost any C# type including collections and managed types, and provides an interface to create custom (de)serializers that can then be used by the default implementation to further increase its capabilities.

# Installation

**GDSerializer** is available as a [NuGet package](https://www.nuget.org/packages/GDSerializer/), which can be installed either through an IDE or by manually including the following lines in a Godot project's `.csproj` file:
```xml
<ItemGroup>
    <PackageReference Include="GDSerializer" Version="3.0.0"/>
</ItemGroup>
```
Its dependencies may need to be installed as well, in a similar fashion.

It is recommended to enable nullability warnings in the project; however, this is completely optional. Again, this can be done either through an IDE or by including the following lines in the `.csproj` file:
```xml
<PropertyGroup>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

# Usage

Creating a new serializer instance:
```csharp
Serializer serializer = new(); // or new Serializer() for older language versions that do not recognise target-typed new()
```

Serializing an object as an XML node:
```csharp
XmlNode xml = serializer.Serialize(obj);
```

Deserializing an XML node as an object:
```csharp
object? obj = serializer.Deserialize(xml);
```

An optional type argument can be provided for both serialization and deserialization, in order to assist the serializer with figuring out the object's `Type`.  
Generic versions of the methods can also be used.

More detailed instructions on using the `Serializer` class can be found on the [GDSerializer wiki](https://github.com/Carnagion/GDSerializer/wiki).

# Versioning

**GDSerializer** uses [Semantic Versioning](https://semver.org/).

For Godot 3.5 users, the latest version is `2.0.3`.
For Godot 4.0 users, the latest version is `3.0.0`.

