namespace Aiva.Admin.Api.Infrastructure.Data.Seeding;

public class DatabaseSeeder
{
  private readonly IEnumerable<IDataSeeder> _seeders;

  public DatabaseSeeder(IEnumerable<IDataSeeder> seeders)
  {
    _seeders = seeders;
  }

  public async Task SeedAllAsync(AppDbContext dbContext)
  {
    // Execute seeders in order
    foreach (var seeder in _seeders.OrderBy(s => s.Order))
    {
      await seeder.SeedAsync(dbContext);
    }
  }
}
