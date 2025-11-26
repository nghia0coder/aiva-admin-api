namespace Aiva.Admin.Api.UnitTests.Core.StorageAggregate;

using Aiva.Admin.Api.Core.StorageAggregate;

public class StorageUpdateName
{
  private readonly StorageName _initialName = StorageName.From("Initial Name");
  private readonly StorageName _newName = StorageName.From("New Name");

  [Fact]
  public void UpdatesName()
  {
    // Arrange
    var storage = new Storage(_initialName, null);

    // Act
    storage.UpdateName(_newName);

    // Assert
    storage.StorageName.ShouldBe(_newName);
  }

  [Fact]
  public void ReturnsSelfWhenNameUnchanged()
  {
    // Arrange
    var storage = new Storage(_initialName, null);

    // Act
    var result = storage.UpdateName(_initialName);

    // Assert
    result.ShouldBeSameAs(storage);
    storage.StorageName.ShouldBe(_initialName);
  }

  [Fact]
  public void ReturnsSelfAfterUpdate()
  {
    // Arrange
    var storage = new Storage(_initialName, null);

    // Act
    var result = storage.UpdateName(_newName);

    // Assert - Fluent API pattern
    result.ShouldBeSameAs(storage);
  }
}
