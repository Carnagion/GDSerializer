# GDSerializer

**GDSerializer** is an XML serialization (and deserialization) library developed with Godot's C# API in mind.

It supports (de)serialization of almost any C# type including collections and managed types, and provides an interface to create custom (de)serializers that can then be used by the default implementation to further increase its capabilities.

# Installation

**GDSerializer** is available as a [NuGet package](https://www.nuget.org/packages/GDSerializer/), which can be installed either through an IDE or by manually including the following lines in a Godot project's `.csproj` file:
```xml
<ItemGroup>
    <PackageReference Include="GDSerializer" Version="1.0.0" />
</ItemGroup>
```
Its dependencies may need to be installed as well, in a similar fashion.

Due to [a bug](https://github.com/godotengine/godot/issues/42271) in Godot, the following lines will also need to be included in the `.csproj` file to properly compile along with NuGet packages:
```xml
<PropertyGroup>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>
```

Note that **GDSerializer** targets `.NET Standard 2.1` whereas Godot projects target `.NET Framework 4.7.2` by default, so the target framework may need to be changed either through an IDE or by manually editing the `.csproj` file like so:
```xml
<TargetFramework>netstandard2.1</TargetFramework>
```

It is also recommended to enable nullability warnings in the project; however, this is completely optional. Again, this can be done either through an IDE or by including the following lines in the `.csproj` file:
```xml
<PropertyGroup>
    <Nullable>enable</Nullable>
</PropertyGroup>
```