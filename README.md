# GDSerializer

GDSerializer is an XML serialization (and deserialization) library developed with Godot's C# API in mind.

It supports (de)serialization of almost any C# type including collections and managed types, and provides an interface to create custom (de)serializers that can then be used by the default implementation to further increase its capabilities.

# Installation

GDSerializer is available as a [NuGet package](https://www.nuget.org/packages/GDSerializer/).  
Simply include the following lines in a Godot project's `.csproj` file (either by editing the file manually or letting an IDE install the package):  
```xml
<ItemGroup>
    <PackageReference Include="GDSerializer" Version="0.2.0" />
</ItemGroup>
```

Due to [a bug](https://github.com/godotengine/godot/issues/42271) in Godot, the following lines will also need to be included in the `.csproj` file to properly compile along with NuGet packages:
```xml
<PropertyGroup>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>
```