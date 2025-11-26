namespace Aiva.Admin.Api.UnitTests.Core.StorageAggregate;

using Api.Core.StorageAggregate;

public class StorageIdFrom
{
  [Fact]
  public void CreatesGivenValidValue()
  {
    // Arrange
    int validValue = 1;

    // Act
    var storageId = StorageId.From(validValue);

    // Assert
    storageId.Value.ShouldBe(validValue);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  [InlineData(-100)]
  public void ThrowsGivenInvalidValue(int invalidValue)
  {
    // Act & Assert
    Should.Throw<Vogen.ValueObjectValidationException>(() => StorageId.From(invalidValue));
  }
}
