namespace Aiva.Admin.Api.UnitTests.Core.StorageAggregate;

using Aiva.Admin.Api.Core.StorageAggregate;

public class StorageConstructor
{
  private readonly StorageName _testName = StorageName.From("Test Storage");
  private const string _testDescription = "Test Description";

  [Fact]
  public void InitializesName()
  {
    // Act
    var storage = new Storage(_testName, _testDescription);

    // Assert
    storage.StorageName.ShouldBe(_testName);
  }

  [Fact]
  public void InitializesDescription()
  {
    // Act
    var storage = new Storage(_testName, _testDescription);

    // Assert
    storage.StorageDescription.ShouldBe(_testDescription);
  }

  [Fact]
  public void AllowsNullDescription()
  {
    // Act
    var storage = new Storage(_testName, null);

    // Assert
    storage.StorageDescription.ShouldBeNull();
  }

  [Fact]
  public void InitializesContainerNameAsEmpty()
  {
    // Act
    var storage = new Storage(_testName, _testDescription);

    // Assert
    storage.ContainerName.ShouldBe(string.Empty);
  }
}
