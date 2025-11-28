namespace Aiva.Admin.Api.UnitTests.Builders;

using System.Reflection;
using Aiva.Admin.Api.Core.ContributorAggregate;

/// <summary>
/// Test Builder for creating Contributor entities with fluent API.
/// Uses reflection to set private/protected properties like Id.
/// </summary>
public class ContributorBuilder
{
  private ContributorName _name = ContributorName.From("default contributor");
  private int _id = 1;
  private ContributorStatus _status = ContributorStatus.NotSet;
  private PhoneNumber? _phoneNumber;

  public ContributorBuilder WithName(string name)
  {
    _name = ContributorName.From(name);
    return this;
  }

  public ContributorBuilder WithName(ContributorName name)
  {
    _name = name;
    return this;
  }

  public ContributorBuilder WithId(int id)
  {
    _id = id;
    return this;
  }

  public ContributorBuilder WithStatus(ContributorStatus status)
  {
    _status = status;
    return this;
  }

  public ContributorBuilder WithPhoneNumber(string countryCode, string number, string? extension = null)
  {
    _phoneNumber = new PhoneNumber(countryCode, number, extension);
    return this;
  }

  public Contributor Build()
  {
    var contributor = new Contributor(_name);

    // Use reflection to set Id from base class
    var idProperty = GetPropertyFromHierarchy(typeof(Contributor), "Id");
    idProperty?.SetValue(contributor, ContributorId.From(_id));

    // Set phone number if provided
    if (_phoneNumber is not null)
    {
      contributor.UpdatePhoneNumber(_phoneNumber);
    }

    return contributor;
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
  /// Creates a default valid Contributor for simple test cases
  /// </summary>
  public static Contributor Default() => new ContributorBuilder().Build();

  /// <summary>
  /// Creates a Contributor with specified Id
  /// </summary>
  public static Contributor WithDefaultId(int id) => new ContributorBuilder().WithId(id).Build();
}

