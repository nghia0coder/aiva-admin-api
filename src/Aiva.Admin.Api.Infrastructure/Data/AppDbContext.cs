namespace Aiva.Admin.Api.Infrastructure.Data;

using Core.ContributorAggregate;
using Core.ConversationAggregate;
using Core.FileAggregate;
using Core.FolderAggregate;
using Core.StorageAggregate;
using Core.SystemPromptAggregate;
using Core.UserAggregate;

// dotnet ef migrations add AddTitleGenerationStatus -c AppDbContext -p src/Aiva.Admin.Api.Infrastructure/Aiva.Admin.Api.Infrastructure.csproj  -s src/Aiva.Admin.Api.Web/Aiva.Admin.Api.Web.csproj  -o Data/Migrations

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<Contributor> Contributors => Set<Contributor>();
  public DbSet<Storage> Storages => Set<Storage>();
  public DbSet<Folder> Folders => Set<Folder>();
  public DbSet<File> Files => Set<File>();
  public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
  public DbSet<Conversation> Conversations => Set<Conversation>();
  public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
  public DbSet<User> Users => Set<User>();
  public DbSet<SystemPrompt> SystemPrompts => Set<SystemPrompt>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}
