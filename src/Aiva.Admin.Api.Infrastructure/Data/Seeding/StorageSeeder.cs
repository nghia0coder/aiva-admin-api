namespace Aiva.Admin.Api.Infrastructure.Data.Seeding;

using Core.StorageAggregate;

public class StorageSeeder : IDataSeeder
{
  public const int NUMBER_OF_STORAGES = 5;

  public static readonly Storage Storage1 = new(StorageName.From("Main Warehouse"), "Description 1");
  public static readonly Storage Storage2 = new(StorageName.From("Cold Storage"), "Description 2");

  public int Order => 20; // Storages seed after Contributors

  public async Task SeedAsync(AppDbContext dbContext)
  {
    if (await dbContext.Storages.AnyAsync()) return;

    dbContext.Storages.AddRange([Storage1, Storage2]);
    await dbContext.SaveChangesAsync();

    for (int i = 1; i <= NUMBER_OF_STORAGES - 2; i++)
    {
      dbContext.Storages.Add(new Storage(StorageName.From($"Storage {i}"), $"Description {i}"));
    }
    await dbContext.SaveChangesAsync();
  }
}
