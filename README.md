# Aiva.Admin.Api

A modern .NET 10 API solution built with Clean Architecture principles, Domain-Driven Design (DDD), and orchestrated with .NET Aspire for cloud-ready microservices development.

>**📝 Note:** This project is based on the [Ardalis Clean Architecture Template](https://github.com/ardalis/CleanArchitecture), I adjusted a little bit in Presentention Layer by group into folder (It's just personal preference). For more information about the template and its design decisions, please visit the official repository.

## 🏗️ Architecture Overview

This project implements **Clean Architecture** with a hybrid approach combining:
- **Clean Architecture** for core business logic and application layers
- **Vertical Slice Architecture** for API endpoints using FastEndpoints
- **Domain-Driven Design (DDD)** patterns in the domain layer
- **.NET Aspire** for distributed application orchestration

### Architectural Layers

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│  ┌────────────────────────────────────────────────────┐ │
│  │  Aiva.Admin.Api.Web (FastEndpoints)                │ │
│  │  - API Endpoints (Vertical Slices)                 │ │
│  │  - Request/Response DTOs                           │ │
│  │  - Validators                                      │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                      │
│  ┌────────────────────────────────────────────────────┐ │
│  │  Aiva.Admin.Api.UseCases (CQRS Pattern)            │ │
│  │  - Commands (Create, Update, Delete)               │ │
│  │  - Queries (Get, List)                             │ │
│  │  - DTOs and Mappers                                │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                     Domain Layer                         │
│  ┌────────────────────────────────────────────────────┐ │
│  │  Aiva.Admin.Api.Core (Business Logic)              │ │
│  │  - Entities & Aggregates                           │ │
│  │  - Value Objects                                   │ │
│  │  - Domain Events                                   │ │
│  │  - Specifications                                  │ │
│  │  - Domain Services                                 │ │
│  │  - Repository Interfaces                           │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                          ▲
                          │
┌─────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                     │
│  ┌────────────────────────────────────────────────────┐ │
│  │  Aiva.Admin.Api.Infrastructure                     │ │
│  │  - EF Core DbContext & Repositories                │ │
│  │  - Database Migrations                             │ │
│  │  - External Service Implementations                │ │
│  │  - Email Services (Papercut/MimeKit)               │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### Supporting Projects

- **Aiva.Admin.Api.AspireHost**: Orchestration layer using .NET Aspire for managing distributed services and dependencies
- **Aiva.Admin.Api.ServiceDefaults**: Shared service configurations (OpenTelemetry, health checks, logging)

## 📁 Project Structure

```
Aiva.Admin.Api/
│
├── src/
│   ├── Aiva.Admin.Api.AspireHost/        # .NET Aspire orchestration
│   │   ├── Program.cs                     # Service orchestration configuration
│   │   └── Properties/launchSettings.json # Launch profiles (including WSL support)
│   │
│   ├── Aiva.Admin.Api.Web/                # API Presentation Layer
│   │   ├── Contributors/                  # Feature: Contributor endpoints (Vertical Slices)
│   │   │   ├── Create/                    # Create contributor endpoint + validator
│   │   │   ├── Update/                    # Update contributor endpoint + validator
│   │   │   ├── Delete/                    # Delete contributor endpoint + validator
│   │   │   ├── Get/                       # Get by ID endpoint + validator
│   │   │   └── List/                      # List contributors endpoint + validator
│   │   ├── Configurations/                # Startup configurations
│   │   └── Program.cs                     # Application entry point
│   │
│   ├── Aiva.Admin.Api.UseCases/           # Application Layer (Use Cases)
│   │   ├── Contributors/                  # Contributor use cases (CQRS)
│   │   │   ├── Create/                    # CreateContributorCommand + Handler
│   │   │   ├── Update/                    # UpdateContributorCommand + Handler
│   │   │   ├── Delete/                    # DeleteContributorCommand + Handler
│   │   │   ├── Get/                       # GetContributorQuery + Handler
│   │   │   ├── List/                      # ListContributorsQuery + Handler
│   │   │   └── ContributorDTO.cs          # Shared DTO
│   │   └── PagedResult.cs                 # Pagination support
│   │
│   ├── Aiva.Admin.Api.Core/               # Domain Layer
│   │   ├── ContributorAggregate/          # Contributor aggregate root
│   │   │   ├── Contributor.cs             # Entity with business rules
│   │   │   ├── ContributorId.cs           # Strongly-typed ID (Value Object)
│   │   │   ├── ContributorName.cs         # Value Object
│   │   │   ├── PhoneNumber.cs             # Value Object
│   │   │   ├── ContributorStatus.cs       # Enumeration
│   │   │   ├── Events/                    # Domain events
│   │   │   ├── Handlers/                  # Domain event handlers
│   │   │   └── Specifications/            # Query specifications
│   │   ├── Interfaces/                    # Repository & service abstractions
│   │   └── Services/                      # Domain services
│   │
│   ├── Aiva.Admin.Api.Infrastructure/     # Infrastructure Layer
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs            # EF Core DbContext
│   │   │   ├── EfRepository.cs            # Generic repository implementation
│   │   │   ├── EventDispatcherInterceptor.cs # Domain event dispatcher
│   │   │   ├── Migrations/                # EF Core migrations
│   │   │   ├── Config/                    # Entity configurations
│   │   │   └── Queries/                   # Read-optimized queries
│   │   └── Email/                         # Email service implementations
│   │
│   └── Aiva.Admin.Api.ServiceDefaults/    # Shared configurations
│       └── Extensions.cs                  # OpenTelemetry, health checks, etc.
│
└── tests/
    ├── Aiva.Admin.Api.UnitTests/          # Unit tests (15 tests)
    │   ├── Core/                          # Domain model tests
    │   └── UseCases/                      # Use case handler tests
    │
    ├── Aiva.Admin.Api.IntegrationTests/   # Integration tests
    │   └── Data/                          # Repository & database tests
    │
    ├── Aiva.Admin.Api.FunctionalTests/    # End-to-end API tests (3 tests)
    │   ├── ApiEndpoints/                  # API endpoint tests
    │   └── CustomWebApplicationFactory.cs # Testcontainers setup
    │
    └── Aiva.Admin.Api.AspireTests/        # Aspire orchestration tests
        └── AspireIntegrationTests.cs      # Service startup & health tests
```

## 🚀 Technologies & Frameworks

### Core Technologies
- **.NET 10** - Latest .NET platform
- **C# 13** - Latest C# language features
- **ASP.NET Core** - Web framework

### Architectural Patterns
- **Clean Architecture** - Separation of concerns with dependency inversion
- **Domain-Driven Design (DDD)** - Rich domain models with aggregates and value objects
- **CQRS** - Command Query Responsibility Segregation pattern
- **Vertical Slice Architecture** - Feature-focused endpoint organization

### Key Libraries & Frameworks

#### API & Presentation
- **FastEndpoints** - High-performance, developer-friendly alternative to MVC controllers
- **Swagger/OpenAPI** - API documentation and testing

#### Application Layer
- **MediatR** - Mediator pattern implementation for CQRS
- **FluentValidation** - Validation library integrated with FastEndpoints
- **Ardalis.Result** - Result pattern for better error handling

#### Domain Layer
- **Ardalis.Specification** - Repository pattern with specification support
- **Ardalis.GuardClauses** - Guard clauses for defensive programming
- **Ardalis.SmartEnum** - Type-safe enum alternatives

#### Infrastructure
- **Entity Framework Core** - ORM for database access
- **SQL Server** - Primary database (via Aspire orchestration)
- **SQLite** - Fallback database for standalone runs
- **MimeKit** - Email sending
- **Papercut SMTP** - Email testing during development

#### Orchestration & Observability
- **.NET Aspire** - Cloud-ready application orchestration
- **OpenTelemetry** - Distributed tracing and metrics
- **Serilog** - Structured logging

#### Testing
- **xUnit** - Test framework
- **FluentAssertions** - Fluent assertion library
- **Testcontainers** - Docker containers for integration testing
- **Moq/NSubstitute** - Mocking libraries

## 🎯 Key Design Principles

### 1. **Clean Architecture**
- **Dependency Rule**: Dependencies point inward toward the domain
- Core business logic has no dependencies on external frameworks
- Infrastructure and UI depend on abstractions defined in Core

### 2. **Domain-Driven Design**
- **Aggregates**: Related entities grouped together (e.g., Contributor aggregate)
- **Value Objects**: Immutable objects representing domain concepts (e.g., ContributorName, PhoneNumber)
- **Domain Events**: Communicate changes within the domain
- **Specifications**: Reusable query logic encapsulated in the domain

### 3. **CQRS Pattern**
- **Commands**: Mutate state (Create, Update, Delete)
- **Queries**: Fetch data without side effects (Get, List)
- Clear separation improves testability and scalability

### 4. **Vertical Slice Architecture (for Endpoints)**
- Each feature endpoint contains:
  - Request/Response models
  - Validators
  - Endpoint handler
- Reduces coupling between features
- Makes features easier to understand and modify

### 5. **Parse, Don't Validate**
- Value objects validate data at construction time
- Invalid objects cannot exist in the system
- Type safety at compile time

## 🛠️ Getting Started

### Prerequisites

- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop** - Required for .NET Aspire containers and Testcontainers
- **Visual Studio 2022 (v17.12+)** or **Visual Studio Code** with C# extensions
- **SQL Server Management Studio (optional)** - For database inspection

### Installation & Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Aiva.Admin.Api
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

### Running the Application

#### Option 1: Run with .NET Aspire (Recommended)

This will start SQL Server and Papercut containers automatically:

```bash
# Set AspireHost as startup project and run
cd src/Aiva.Admin.Api.AspireHost
dotnet run
```

The Aspire Dashboard will open in your browser showing:
- **SQL Server** container (port 1433)
- **Papercut SMTP** server (SMTP: port 25, UI: port 37408)
- **Web API** application
- Health checks, metrics, and distributed tracing

**Access Points:**
- Aspire Dashboard: `https://localhost:17021` (or as shown in console)
- Web API: `https://localhost:7071` or `http://localhost:5071`
- Swagger UI: `https://localhost:7071/swagger`
- Papercut Email UI: `http://localhost:37408`

#### Option 2: Run Web API Standalone

This will use SQLite as the database:

```bash
cd src/Aiva.Admin.Api.Web
dotnet run
```

**Access Points:**
- Web API: `https://localhost:7071`
- Swagger UI: `https://localhost:7071/swagger`

#### Option 3: Visual Studio

1. Set `Aiva.Admin.Api.AspireHost` as the startup project
2. Press F5 or click "Start Debugging"
3. The Aspire Dashboard will automatically open

### Database Migrations

Migrations are applied automatically on application startup. To create new migrations:

```bash
# From the solution root
cd src/Aiva.Admin.Api.Web

# Create a new migration
dotnet ef migrations add MigrationName \
  -c AppDbContext \
  -p ../Aiva.Admin.Api.Infrastructure/Aiva.Admin.Api.Infrastructure.csproj \
  -s Aiva.Admin.Api.Web.csproj \
  -o Data/Migrations

# Apply migrations manually (if needed)
dotnet ef database update \
  -c AppDbContext \
  -p ../Aiva.Admin.Api.Infrastructure/Aiva.Admin.Api.Infrastructure.csproj \
  -s Aiva.Admin.Api.Web.csproj
```

### Running with WSL (Windows Subsystem for Linux)

If you're using Docker in WSL, use the WSL-specific launch profiles:

```bash
# From AspireHost directory
dotnet run --launch-profile https-wsl
```

Or in Visual Studio, select the `https-wsl` profile from the dropdown.

See [Aspire Host README](src/Aiva.Admin.Api.AspireHost/README.md) for detailed WSL configuration.

## 🧪 Testing Strategy

The solution includes comprehensive testing at multiple levels:

### Test Projects

1. **Unit Tests** (`Aiva.Admin.Api.UnitTests`) - 15 tests
   - Domain model tests (entities, value objects)
   - Use case handler tests (isolated with mocks)
   - Fast execution, no external dependencies

2. **Integration Tests** (`Aiva.Admin.Api.IntegrationTests`)
   - Repository tests with real database
   - Data access logic validation
   - EF Core behavior verification

3. **Functional Tests** (`Aiva.Admin.Api.FunctionalTests`) - 3 tests
   - End-to-end API endpoint testing
   - Uses **Testcontainers** with SQL Server 2022
   - Each test gets isolated containerized database
   - Validates migrations work correctly

4. **Aspire Tests** (`Aiva.Admin.Api.AspireTests`)
   - Service orchestration tests
   - Container startup and health verification

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Aiva.Admin.Api.UnitTests

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests in parallel (configured via .runsettings)
dotnet test --settings .runsettings
```

### Parallel Test Execution

The solution is configured for parallel test execution:
- Test assemblies run in parallel (uses all CPU cores)
- Test collections within each assembly also run in parallel
- See [PARALLEL_TEST_EXECUTION.md](PARALLEL_TEST_EXECUTION.md) for configuration details

### Testcontainers Implementation

Functional tests use Testcontainers for realistic database testing:
- Real SQL Server 2022 running in Docker containers
- Automatic container lifecycle management
- Isolated database per test class
- See [TESTCONTAINERS_IMPLEMENTATION.md](TESTCONTAINERS_IMPLEMENTATION.md) for details

**Requirements:**
- Docker Desktop must be running
- First run downloads SQL Server image (~1.5 GB)

## 📋 API Endpoints

All endpoints follow RESTful conventions and are documented via Swagger.

### Contributors API

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/contributors` | List all contributors (with pagination) |
| GET | `/contributors/{id}` | Get contributor by ID |
| POST | `/contributors` | Create new contributor |
| PUT | `/contributors/{id}` | Update existing contributor |
| DELETE | `/contributors/{id}` | Delete contributor |

**Sample Request (Create Contributor):**
```json
POST /contributors
{
  "name": "John Doe",
  "phoneNumber": "+1234567890",
  "status": 1
}
```

**Sample Response:**
```json
{
  "id": 1,
  "name": "John Doe",
  "phoneNumber": "+1234567890",
  "status": 1
}
```

Access the interactive API documentation at `/swagger` when running the application.

## 📚 Documentation

Additional documentation files:

- [Aspire Host Configuration](src/Aiva.Admin.Api.AspireHost/README.md) - .NET Aspire setup and WSL support
- [Core Domain Layer](src/Aiva.Admin.Api.Core/README.md) - Domain model guidelines
- [Infrastructure Layer](src/Aiva.Admin.Api.Infrastructure/README.md) - Database and external services
- [Use Cases Layer](src/Aiva.Admin.Api.UseCases/README.md) - Application logic patterns
- [Testcontainers Setup](TESTCONTAINERS_IMPLEMENTATION.md) - Functional testing with Docker
- [Parallel Test Execution](PARALLEL_TEST_EXECUTION.md) - Test performance optimization

## 🔧 Development Workflow

### Adding a New Feature (e.g., "Projects")

1. **Create Domain Entities** (`Core` project)
   ```
   Core/ProjectAggregate/
   ├── Project.cs              # Aggregate root
   ├── ProjectId.cs            # Value object
   ├── ProjectName.cs          # Value object
   ├── Events/                 # Domain events
   └── Specifications/         # Query specifications
   ```

2. **Add Database Configuration** (`Infrastructure` project)
   ```csharp
   // Infrastructure/Data/Config/ProjectConfiguration.cs
   public class ProjectConfiguration : IEntityTypeConfiguration<Project>
   ```

3. **Create Use Cases** (`UseCases` project)
   ```
   UseCases/Projects/
   ├── ProjectDTO.cs
   ├── Create/
   │   ├── CreateProjectCommand.cs
   │   └── CreateProjectHandler.cs
   ├── List/
   │   ├── ListProjectsQuery.cs
   │   └── ListProjectsHandler.cs
   └── ...
   ```

4. **Add API Endpoints** (`Web` project)
   ```
   Web/Projects/
   ├── Create/
   │   ├── Create.cs              # FastEndpoint
   │   ├── CreateProjectRequest.cs
   │   ├── CreateProjectResponse.cs
   │   └── CreateProjectValidator.cs
   ├── List/
   └── ...
   ```

5. **Write Tests**
   - Unit tests for domain logic
   - Integration tests for repositories
   - Functional tests for API endpoints

6. **Create Migration**
   ```bash
   dotnet ef migrations add AddProjectAggregate
   ```

### Code Style Guidelines

- Follow C# coding conventions
- Use meaningful names (domain language)
- Keep methods small and focused
- Write tests before or alongside implementation
- Use guard clauses for validation
- Leverage value objects to avoid primitive obsession

## 🏢 Enterprise Best Practices

This solution demonstrates enterprise-grade practices:

✅ **Separation of Concerns** - Clear boundaries between layers  
✅ **Dependency Inversion** - Core depends on abstractions, not implementations  
✅ **Testability** - High test coverage with fast unit tests  
✅ **Maintainability** - Features organized by vertical slices  
✅ **Observability** - OpenTelemetry integration for distributed tracing  
✅ **Container Ready** - Docker support with .NET Aspire  
✅ **Type Safety** - Value objects and strongly-typed IDs  
✅ **Domain-Centric** - Business logic in domain layer, not scattered  
✅ **CQRS** - Optimized read and write paths  
✅ **Realistic Testing** - Testcontainers for database integration tests  

## 🤝 Contributing

1. Create a feature branch
2. Write tests for new functionality
3. Ensure all tests pass
4. Follow existing code structure and naming conventions
5. Submit a pull request

## 📄 License

See [LICENSE](LICENSE) file for details.

## 🔗 Resources & References

- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Ardalis Clean Architecture Template](https://github.com/ardalis/CleanArchitecture)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [Domain-Driven Design](https://www.domainlanguage.com/ddd/)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Testcontainers](https://dotnet.testcontainers.org/)

## 📞 Support

For questions or support:
- Check existing documentation in project README files
- Review architectural decision records
- Contact: [NimblePros](https://nimblepros.com)

---

**Built with ❤️ using Clean Architecture principles and modern .NET practices**

