# Clean Architecture Aspire Host

This project uses .NET Aspire to orchestrate the application and its dependencies.

## SQL Server Container

The Aspire host is configured to run a SQL Server container and automatically provides the connection string to the Web application.

### Running the Application

1. Set `Aiva.Admin.Api.AspireHost` as the startup project
2. Run the application (F5 or Ctrl+F5)
3. The Aspire Dashboard will open, showing all running resources including the SQL Server container
4. The Web application will automatically connect to the SQL Server container

### Connection String

When running through Aspire, the connection string is automatically provided by Aspire and will override the `DefaultConnection` in appsettings.json. The connection is named "cleanarchitecture" and is referenced in the Web project.

### Running Without Aspire

If you run the Web project directly (not through AspireHost), it will fall back to using the SQLite connection string from appsettings.json.

### Database Migrations

The existing migrations were created for SQLite but will work with SQL Server as well. If you need to create a new migration:

From the Web project directory:
```bash
dotnet ef migrations add MigrationName -c AppDbContext -p ../Aiva.Admin.Api.Infrastructure/Aiva.Admin.Api.Infrastructure.csproj -s Aiva.Admin.Api.Web.csproj -o Data/Migrations
```

To update the database:
```bash
dotnet ef database update -c AppDbContext -p ../Aiva.Admin.Api.Infrastructure/Aiva.Admin.Api.Infrastructure.csproj -s Aiva.Admin.Api.Web.csproj
```

Note: When running through Aspire, the database will be automatically created in the SQL Server container if it doesn't exist.

### Container Persistence

The SQL Server container is configured with `ContainerLifetime.Persistent`, which means the data will persist between application runs. To reset the database, you can:
1. Delete the container through the Aspire dashboard
2. Use the Docker CLI: `docker rm <container-name>`

## Running with WSL (Windows Subsystem for Linux)

If you're running .NET Aspire from within WSL or need to use Docker that's running in WSL, you can configure Aspire to use the WSL container runtime.

### Prerequisites

1. **Docker Desktop with WSL 2 Integration** (Recommended):
   - Install Docker Desktop for Windows
   - Enable WSL 2 integration in Docker Desktop Settings > Resources > WSL Integration
   - Enable integration for your WSL distribution

2. **OR Docker Engine in WSL**:
   - Install Docker directly in your WSL distribution
   - Ensure Docker daemon is running: `sudo service docker start`

### Configuration

The `launchSettings.json` includes two profiles for WSL:

- **`https-wsl`**: HTTPS profile configured for WSL Docker
- **`http-wsl`**: HTTP profile configured for WSL Docker

These profiles set:
- `DOCKER_HOST=unix:///var/run/docker.sock` - Points to Docker socket in WSL
- `ASPIRE_CONTAINER_RUNTIME=docker` - Explicitly tells Aspire to use Docker

### Using WSL Profiles

1. **In Visual Studio**: Select the `https-wsl` or `http-wsl` profile from the debug dropdown
2. **From Command Line**: 
   ```bash
   dotnet run --launch-profile https-wsl
   ```

### Alternative: Environment Variables

You can also set these environment variables globally in your WSL shell:

```bash
export DOCKER_HOST=unix:///var/run/docker.sock
export ASPIRE_CONTAINER_RUNTIME=docker
```

Or add them to your `~/.bashrc` or `~/.zshrc`:

```bash
echo 'export DOCKER_HOST=unix:///var/run/docker.sock' >> ~/.bashrc
echo 'export ASPIRE_CONTAINER_RUNTIME=docker' >> ~/.bashrc
source ~/.bashrc
```

### Verifying Docker Connection

Before running Aspire, verify Docker is accessible:

```bash
docker ps
```

If this command works, Aspire should be able to use the container runtime.

### Troubleshooting

- **Docker not found**: Ensure Docker is installed and running in WSL
- **Permission denied**: You may need to add your user to the docker group:
  ```bash
  sudo usermod -aG docker $USER
  ```
  Then log out and back in, or restart WSL
- **Connection refused**: Ensure Docker daemon is running:
  ```bash
  sudo service docker status
  sudo service docker start  # if not running
  ```
