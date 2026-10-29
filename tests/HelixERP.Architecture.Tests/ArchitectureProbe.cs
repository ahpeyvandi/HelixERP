using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace HelixERP.Architecture.Tests;

internal static class ArchitectureProbe
{
    public static IReadOnlyCollection<string> ReferencedTypes(Assembly assembly)
    {
        if (!string.IsNullOrEmpty(assembly.Location) && File.Exists(assembly.Location))
        {
            return ReadMetadataTypeReferences(assembly.Location);
        }

        return ReadReflectionTypeReferences(assembly);
    }

    public static bool References(Assembly assembly, string prefix)
        => ReferencedTypes(assembly).Any(typeName => typeName.StartsWith(prefix, StringComparison.Ordinal));

    private static IReadOnlyCollection<string> ReadMetadataTypeReferences(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var metadataReader = peReader.GetMetadataReader();
        var typeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var handle in metadataReader.TypeReferences)
        {
            var typeReference = metadataReader.GetTypeReference(handle);
            var typeNamespace = metadataReader.GetString(typeReference.Namespace);
            var typeName = metadataReader.GetString(typeReference.Name);

            typeNames.Add(string.IsNullOrEmpty(typeNamespace) ? typeName : $"{typeNamespace}.{typeName}");
        }

        return typeNames;
    }

    private static IReadOnlyCollection<string> ReadReflectionTypeReferences(Assembly assembly)
    {
        var typeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in GetLoadableTypes(assembly))
        {
            AddType(typeNames, type.BaseType);

            foreach (var @interface in type.GetInterfaces())
            {
                AddType(typeNames, @interface);
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                AddType(typeNames, field.FieldType);
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                AddType(typeNames, property.PropertyType);
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                AddType(typeNames, method.ReturnType);

                foreach (var parameter in method.GetParameters())
                {
                    AddType(typeNames, parameter.ParameterType);
                }
            }
        }

        return typeNames;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null)!;
        }
    }

    private static void AddType(HashSet<string> typeNames, Type? type)
    {
        if (type is null || type.IsGenericParameter)
        {
            return;
        }

        if (type.HasElementType)
        {
            AddType(typeNames, type.GetElementType());
            return;
        }

        if (type.IsGenericType)
        {
            AddType(typeNames, type.GetGenericTypeDefinition());

            foreach (var argument in type.GetGenericArguments())
            {
                AddType(typeNames, argument);
            }

            return;
        }

        if (type.FullName is not null)
        {
            typeNames.Add(type.FullName);
        }
    }
}