namespace Aiva.Admin.Api.UnitTests.UseCases.Storages;

using Aiva.Admin.Api.Core.StorageAggregate;
using Aiva.Admin.Api.UseCases.Contributors.Create;
using Aiva.Admin.Api.UseCases.Storages.Create;

public class CreateStorageHandlerHandle
{
  private readonly StorageName _testName = StorageName.From("test storage");
  private readonly string _testDescription = "test description";
  private readonly IRepository<Storage> _repository = Substitute.For<IRepository<Storage>>();
  private readonly CreateStorageHandler _handler;

  public CreateStorageHandlerHandle()
  {
    _handler = new CreateStorageHandler(_repository);
  }

  private Storage CreateStorage()
  {
    return new Storage(_testName, _testDescription);
  }

  [Fact]
  public async Task ReturnsSuccessGivenValidInput()
  {
    // Arrange
    _repository.AddAsync(Arg.Any<Storage>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(CreateStorage()));

    var command = new CreateStorageCommand(_testName, _testDescription);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
  }

  [Fact]
  public async Task CallsRepositoryAddAsync()
  {
    // Arrange
    _repository.AddAsync(Arg.Any<Storage>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(CreateStorage()));

    var command = new CreateStorageCommand(_testName, _testDescription);

    // Act
    await _handler.Handle(command, CancellationToken.None);

    // Assert
    await _repository.Received(1).AddAsync(
      Arg.Is<Storage>(s => s.StorageName == _testName && s.StorageDescription == _testDescription),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AllowsNullDescription()
  {
    // Arrange
    var storageWithNullDesc = new Storage(_testName, null);
    _repository.AddAsync(Arg.Any<Storage>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(storageWithNullDesc));

    var command = new CreateStorageCommand(_testName, null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
  }
}
