namespace Aiva.Admin.Api.Infrastructure.Data.Seeding;

public interface IDataSeeder
{
  /// <summary>
  /// Order of execution. Lower values run first.
  /// Use this for dependencies (e.g., User must seed before Order)
  /// </summary>
  int Order { get; }

  /// <summary>
  /// Seed data for this entity/aggregate
  /// </summary>
  Task SeedAsync(AppDbContext dbContext);
}
