namespace Aiva.Admin.Api.UnitTests.Builders;

using System.Reflection;
using Aiva.Admin.Api.Core.StorageAggregate;

/// <summary>
/// Test Builder for creating Storage entities with fluent API.
/// Uses reflection to set private/protected properties like Id.
/// </summary>
public class StorageBuilder
{
    private StorageName _name = StorageName.From("default storage");
    private string? _description = "default description";
    private int _id = 1;
    private string _containerName = string.Empty;
    private bool _isContainerProvisioned = false;

    public StorageBuilder WithName(string name)
    {
        _name = StorageName.From(name);
        return this;
    }

    public StorageBuilder WithName(StorageName name)
    {
        _name = name;
        return this;
    }

    public StorageBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public StorageBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public StorageBuilder WithContainerName(string containerName)
    {
        _containerName = containerName;
        return this;
    }

    public StorageBuilder WithContainerProvisioned(bool isProvisioned = true)
    {
        _isContainerProvisioned = isProvisioned;
        return this;
    }

    public Storage Build()
    {
        var storage = new Storage(_name, _description);

        // Use reflection to set Id from base class (EntityBase<Storage, StorageId>)
        // Need to search through inheritance hierarchy for the Id property
        var idProperty = GetPropertyFromHierarchy(typeof(Storage), "Id");
        idProperty?.SetValue(storage, StorageId.From(_id));

        // Set other properties
        if (!string.IsNullOrEmpty(_containerName))
        {
            storage.ContainerName = _containerName;
        }

        if (_isContainerProvisioned)
        {
            storage.MarkContainerProvisioned();
        }

        return storage;
    }

    /// <summary>
    /// Gets a property from the type or any of its base types
    /// </summary>
    private static PropertyInfo? GetPropertyFromHierarchy(Type type, string propertyName)
    {
        var currentType = type;
        while (currentType != null)
        {
            var property = currentType.GetProperty(
              propertyName,
              BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (property != null)
                return property;

            currentType = currentType.BaseType;
        }

        // Fallback: try without DeclaredOnly
        return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }

    /// <summary>
    /// Creates a default valid Storage for simple test cases
    /// </summary>
    public static Storage Default() => new StorageBuilder().Build();

    /// <summary>
    /// Creates a Storage with specified Id
    /// </summary>
    public static Storage WithDefaultId(int id) => new StorageBuilder().WithId(id).Build();
}

