namespace Aiva.Admin.Api.UnitTests.Core.StorageAggregate;

using Aiva.Admin.Api.Core.StorageAggregate;

public class StorageNameFrom
{
  [Fact]
  public void CreatesGivenValidValue()
  {
    // Arrange
    string validValue = "My Storage";

    // Act
    var storageName = StorageName.From(validValue);

    // Assert
    storageName.Value.ShouldBe(validValue);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  public void ThrowsGivenEmptyOrNullValue(string? invalidValue)
  {
    // Act & Assert
    Should.Throw<Vogen.ValueObjectValidationException>(() => StorageName.From(invalidValue!));
  }

  [Fact]
  public void ThrowsGivenTooLongValue()
  {
    // Arrange
    string tooLongValue = new string('a', StorageName.MaxLength + 1);

    // Act & Assert
    Should.Throw<Vogen.ValueObjectValidationException>(() => StorageName.From(tooLongValue));
  }

  [Fact]
  public void AcceptsMaxLengthValue()
  {
    // Arrange
    string maxLengthValue = new string('a', StorageName.MaxLength);

    // Act
    var storageName = StorageName.From(maxLengthValue);

    // Assert
    storageName.Value.ShouldBe(maxLengthValue);
  }
}
