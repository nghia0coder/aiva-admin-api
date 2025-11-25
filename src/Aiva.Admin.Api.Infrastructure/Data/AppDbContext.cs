namespace Aiva.Admin.Api.Infrastructure.Data;

using Core.ContributorAggregate;
using Core.StorageAggregate;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<Contributor> Contributors => Set<Contributor>();
  public DbSet<Storage> Storages => Set<Storage>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}
