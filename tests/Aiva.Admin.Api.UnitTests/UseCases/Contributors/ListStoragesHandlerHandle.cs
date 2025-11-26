namespace Aiva.Admin.Api.UnitTests.UseCases.Storages;

using Api.Core.StorageAggregate;
using Api.UseCases;
using Api.UseCases.Storages;
using Api.UseCases.Storages.List;

public class ListStoragesHandlerHandle
{
  private readonly IListStoragesQueryService _queryService = Substitute.For<IListStoragesQueryService>();
  private readonly ListStoragesHandler _handler;

  public ListStoragesHandlerHandle()
  {
    _handler = new ListStoragesHandler(_queryService);
  }

  [Fact]
  public async Task ReturnsSuccessWithPagedResult()
  {
    // Arrange
    var expectedResult = new PagedResult<StorageDto>(
      Items: new List<StorageDto>
      {
        new(StorageId.From(1), StorageName.From("Storage 1"), "Desc 1"),
        new(StorageId.From(2), StorageName.From("Storage 2"), "Desc 2")
      },
      Page: 1,
      PerPage: 10,
      TotalCount: 2,
      TotalPages: 1);

    _queryService.ListAsync(Arg.Any<int>(), Arg.Any<int>())
      .Returns(Task.FromResult(expectedResult));

    var query = new ListStoragesQuery(Page: 1, PerPage: 10);

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.TotalCount.ShouldBe(2);
    result.Value.Items.Count().ShouldBe(2);
  }

  [Fact]
  public async Task UsesDefaultPaginationWhenNotProvided()
  {
    // Arrange
    var expectedResult = new PagedResult<StorageDto>(Items: [], Page: 1, PerPage: 10, TotalCount: 0, TotalPages: 0);
    _queryService.ListAsync(Arg.Any<int>(), Arg.Any<int>())
      .Returns(Task.FromResult(expectedResult));

    var query = new ListStoragesQuery(); // null values

    // Act
    await _handler.Handle(query, CancellationToken.None);

    // Assert
    await _queryService.Received(1).ListAsync(1, Constants.DEFAULT_PAGE_SIZE);
  }

  [Fact]
  public async Task ReturnsEmptyResultWhenNoStorages()
  {
    // Arrange
    var emptyResult = new PagedResult<StorageDto>(Items: [], Page: 1, PerPage: 10, TotalCount: 0, TotalPages: 0);
    _queryService.ListAsync(Arg.Any<int>(), Arg.Any<int>())
      .Returns(Task.FromResult(emptyResult));

    var query = new ListStoragesQuery();

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.TotalCount.ShouldBe(0);
    result.Value.Items.ShouldBeEmpty();
  }
}
