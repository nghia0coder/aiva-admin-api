namespace Aiva.Admin.Api.Infrastructure.Data;

using Core.ContributorAggregate;
using Core.FolderAggregate;
using Core.StorageAggregate;

// dotnet ef migrations add AddFolderTable -c AppDbContext -p src/Aiva.Admin.Api.Infrastructure/Aiva.Admin.Api.Infrastructure.csproj  -s src/Aiva.Admin.Api.Web/Aiva.Admin.Api.Web.csproj  -o Data/Migrations

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<Contributor> Contributors => Set<Contributor>();
  public DbSet<Storage> Storages => Set<Storage>();
  public DbSet<Folder> Folders => Set<Folder>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}
