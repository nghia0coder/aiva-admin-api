using Aiva.Admin.Api.Core.StorageAggregate;

namespace Aiva.Admin.Api.IntegrationTests.Data.Storages;

public class EfRepositoryAddTests : BaseEfRepoTestFixture
{
  [Fact]
  public async Task AddStorageAndSetsId()
  {
    // Arrange
    var testStorageName = StorageName.From("testStorage");
    var testDescription = "Test Description";
    var repository = GetRepository<Storage>();
    var storage = new Storage(testStorageName, testDescription);

    // Act
    await repository.AddAsync(storage);

    // Assert
    var newStorage = (await repository.ListAsync()).FirstOrDefault();

    newStorage.ShouldNotBeNull();
    newStorage.StorageName.ShouldBe(testStorageName);
    newStorage.StorageDescription.ShouldBe(testDescription);
    newStorage.Id.Value.ShouldBeGreaterThan(0);
  }

  [Fact]
  public async Task AddStorageWithNullDescription()
  {
    var testStorageName = StorageName.From("testStorage");
    var repository = GetRepository<Storage>();
    var storage = new Storage(testStorageName, null);

    // Act
    await repository.AddAsync(storage);

    // Assert
    var newStorage = (await repository.ListAsync()).FirstOrDefault();

    newStorage.ShouldNotBeNull();
    newStorage.StorageDescription.ShouldBeNull();
  }
}
