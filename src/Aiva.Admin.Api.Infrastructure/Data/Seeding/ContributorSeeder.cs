using Aiva.Admin.Api.Core.ContributorAggregate;

namespace Aiva.Admin.Api.Infrastructure.Data.Seeding;

public class ContributorSeeder : IDataSeeder
{
  public const int NUMBER_OF_CONTRIBUTORS = 27;

  // Test data could reference from Functional Tests
  public static readonly Contributor Contributor1 = new(ContributorName.From("Ardalis"));
  public static readonly Contributor Contributor2 = new(ContributorName.From("Ilyana"));

  public int Order => 10; // Contributors seed first

  public async Task SeedAsync(AppDbContext dbContext)
  {
    if (await dbContext.Contributors.AnyAsync()) return;

    dbContext.Contributors.AddRange([Contributor1, Contributor2]);
    await dbContext.SaveChangesAsync();

    // Add more for pagination demo
    for (int i = 1; i <= NUMBER_OF_CONTRIBUTORS - 2; i++)
    {
      dbContext.Contributors.Add(new Contributor(ContributorName.From($"Contributor {i}")));
    }
    await dbContext.SaveChangesAsync();
  }
}
