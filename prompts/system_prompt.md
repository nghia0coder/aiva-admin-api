# 🤖 E-commerce AI Chatbot with RAG Architecture

## 📋 Table of Contents
- [Project Overview](#project-overview)
- [Key Use Cases](#key-use-cases)
- [Technical Architecture](#technical-architecture)
- [RAG Processing Flow](#rag-processing-flow)
- [API Specifications](#api-specifications)
- [Data Management](#data-management)
- [Security & Compliance](#security--compliance)
- [Performance & Scalability](#performance--scalability)
- [Monitoring & Observability](#monitoring--observability)
- [Testing Strategy](#testing-strategy)
- [AI Agent Guidelines](#ai-agent-guidelines)
- [Development Standards](#development-standards)
- [Deployment & CI/CD](#deployment--cicd)

---

## 🎯 Project Overview

### Mission Statement
Build an enterprise-grade E-commerce AI Chatbot that serves both customers and internal teams, leveraging Azure's AI ecosystem to deliver intelligent, grounded, and factual responses through a Retrieval-Augmented Generation (RAG) pipeline.

### Core Objectives

1. **Intelligent Data Integration**
   - Azure Blob Storage: Unstructured data (PDF, CSV, JSON, documents)
   - Azure SQL Database: Transactional data (orders, products, customers, inventory)
   - Azure Cosmos DB: NoSQL data (user profiles, sessions, analytics)
   - Azure AI Search: Unified vector + hybrid search index

2. **RAG Pipeline Implementation**
   - Implement retrieval-augmented generation for grounded answers
   - Minimize hallucinations through context-aware responses
   - Support multi-source data retrieval and fusion
   - Enable real-time and cached responses

3. **Robust Backend API**
   - Receive and validate user queries
   - Orchestrate multi-source data retrieval
   - Manage Azure OpenAI interactions
   - Return structured, typed responses (JSON/TypeScript interfaces)
   - Support streaming responses for better UX

4. **Enterprise Integration**
   - RESTful API with comprehensive documentation (OpenAPI/Swagger)
   - WebSocket support for real-time chat
   - Authentication and authorization (Azure AD B2C/Entra ID)
   - Rate limiting and quota management
   - Multi-tenant architecture support

---

## 📌 Key Use Cases

### 1. Customer-Facing Chatbot

**Order Management**
- Real-time order status tracking
- Order history retrieval
- Delivery estimates and tracking
- Order modification requests

**Product Intelligence**
- Detailed product information and specifications
- Pricing and availability checks
- Product comparisons
- Cross-sell and upsell recommendations

**Customer Service**
- Return and refund policy guidance
- Warranty information
- Shipping and delivery options
- FAQ and help center navigation

**Personalization**
- Personalized product recommendations based on purchase history
- Wishlist management
- Promotional offers and discounts
- Loyalty program information

### 2. Internal Business Assistant

**Data Analytics & Reporting**
- Revenue and sales analytics
- Customer behavior insights
- Inventory turnover reports
- Performance KPI queries

**Document Retrieval**
- Employee handbooks and policies
- Standard Operating Procedures (SOPs)
- Training materials and certifications
- Compliance documents

**Operational Queries**
- Vendor information and contacts
- Supply chain status
- Team schedules and availability
- Project documentation

### 3. Extended Workflows

**Document Processing**
- Automated document summarization
- Content extraction and classification
- Multi-document synthesis
- Semantic search across document collections

**Data Pipeline Automation**
- Automated ingestion from Blob Storage to Azure AI Search
- Real-time index updates
- Data validation and cleansing
- Schema evolution handling

**Advanced Analytics**
- Sentiment analysis on customer feedback
- Trend detection and forecasting
- Anomaly detection in sales patterns
- Churn prediction and prevention

---

## ⚙️ Technical Architecture

### Azure Services Stack

#### 1. **Azure Blob Storage**
- **Purpose**: Store raw and unstructured documents
- **File Types**: PDF, CSV, JSON, DOCX, images, videos
- **Organization**: Container-based structure by data type
- **Access**: Managed Identity for secure access
- **Lifecycle**: Automated tiering (Hot → Cool → Archive)

#### 2. **Azure SQL Database**
- **Purpose**: Transactional and relational data
- **Schema**: Orders, Products, Customers, Inventory, Users
- **Features**: Connection pooling, query optimization, indexing
- **Backup**: Automated daily backups with point-in-time restore
- **Scaling**: Elastic pools for multi-tenant scenarios

#### 3. **Azure Cosmos DB**
- **Purpose**: NoSQL data for high-throughput scenarios
- **Models**: User sessions, chat history, analytics events
- **Consistency**: Session consistency for chat continuity
- **Partitioning**: By user_id or tenant_id
- **Change Feed**: Real-time sync to Azure AI Search

#### 4. **Azure AI Search**
- **Purpose**: Unified search index with vector and hybrid capabilities
- **Indexing Strategy**:
  - Vector search for semantic similarity
  - Full-text search for exact matches
  - Hybrid search combining both
- **Skillsets**: OCR, entity recognition, key phrase extraction
- **Enrichment Pipeline**: Automated document processing
- **Index Schema**: Unified schema across data sources

#### 5. **Azure OpenAI Service**
- **Models**: 
  - GPT-4o for complex reasoning
  - GPT-4 Turbo for cost-effective queries
  - Ada-002 for embeddings
- **Features**: Function calling, streaming, JSON mode
- **Deployment**: Multiple deployments for load balancing
- **Token Management**: Input/output token optimization

#### 6. **Backend Services**
- **Technology**: .NET 10 with C# 13 (current stack)
- **Architecture**: Clean Architecture with Vertical Slices
- **API**: ASP.NET Core Web API with minimal APIs
- **Patterns**: CQRS with MediatR, Repository pattern
- **Containerization**: Docker with multi-stage builds
- **Hosting**: Azure Container Apps or Azure Kubernetes Service

#### 7. **Supporting Services**
- **Azure API Management**: API gateway, rate limiting, analytics
- **Azure Application Insights**: Telemetry and monitoring
- **Azure Key Vault**: Secrets and certificate management
- **Azure Service Bus**: Event-driven messaging
- **Azure Cache for Redis**: Response caching and session state

### Architecture Patterns

#### Clean Architecture Layers
```
┌─────────────────────────────────────────┐
│         Web API (Presentation)          │
│  Controllers, Endpoints, Middleware     │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│      UseCases (Application Logic)       │
│   Commands, Queries, Handlers, DTOs    │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│      Core (Domain/Business Logic)       │
│   Entities, Aggregates, Domain Events  │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│     Infrastructure (External Deps)      │
│  Data Access, Azure Services, Email     │
└─────────────────────────────────────────┘
```

---

## 📡 RAG Processing Flow

### High-Level Flow
```
User Query → API Gateway → Intent Classification → Multi-Source Retrieval → 
Context Assembly → Azure OpenAI → Response Validation → Client Response
```

### Detailed Pipeline Steps

#### Step 1: Query Reception & Validation
- Receive user query via REST or WebSocket
- Validate input (length, content, format)
- Apply rate limiting and authentication
- Log request with correlation ID

#### Step 2: Intent Classification & Routing
```csharp
// Determine query intent
if (RequiresDocumentSearch(query))
    sources.Add(DataSource.AzureAISearch);
    
if (RequiresDatabaseQuery(query))
    sources.Add(DataSource.AzureSQL);
    
if (RequiresUserContext(query))
    sources.Add(DataSource.CosmosDB);
```

#### Step 3: Parallel Data Retrieval
- **Vector Search**: Query Azure AI Search for semantic matches
  - Generate query embedding using Ada-002
  - Retrieve top-k relevant documents (k=5-10)
  - Apply relevance score threshold (>0.7)
  
- **Structured Query**: Execute SQL or Cosmos queries
  - Parse intent to SQL/NoSQL query
  - Apply row limits and timeout constraints
  - Return structured results
  
- **User Context**: Fetch user profile, history, preferences
  - Recent chat history (last 5-10 messages)
  - User preferences and settings
  - Transaction history

#### Step 4: Context Assembly & Optimization
```csharp
var prompt = new PromptBuilder()
    .AddSystemMessage(systemPrompt)
    .AddUserContext(userProfile)
    .AddRetrievedDocuments(searchResults)
    .AddStructuredData(sqlResults)
    .AddConversationHistory(chatHistory)
    .AddUserQuery(query)
    .Build();
    
// Optimize token count
if (prompt.TokenCount > MaxTokens)
    prompt = OptimizeContext(prompt);
```

#### Step 5: Azure OpenAI Inference
- Send optimized prompt to Azure OpenAI
- Use function calling for structured actions
- Stream response for better UX
- Apply temperature based on query type (0.0 for factual, 0.7 for creative)

#### Step 6: Response Processing & Validation
- Parse and validate OpenAI response
- Check for hallucination indicators
- Verify facts against retrieved data
- Format response (JSON, Markdown, HTML)
- Add citations and sources

#### Step 7: Response Delivery & Logging
- Return structured response to client
- Log interaction for analytics
- Update conversation history
- Cache response if appropriate

### Error Handling & Fallbacks
```csharp
try {
    // Primary RAG flow
} catch (SearchException ex) {
    // Fallback to database-only query
} catch (OpenAIException ex) {
    // Return pre-defined response or retry with backoff
} catch (Exception ex) {
    // Generic error handling with user-friendly message
}
```

---

## 🔌 API Specifications

### REST Endpoints

#### Chat Endpoints
```http
POST /api/chat/query
POST /api/chat/stream
GET  /api/chat/history/{userId}
DELETE /api/chat/history/{userId}
```

#### Query Management
```http
GET  /api/queries/{queryId}
POST /api/queries/feedback
GET  /api/queries/analytics
```

#### User Management
```http
GET  /api/users/{userId}/profile
PUT  /api/users/{userId}/preferences
GET  /api/users/{userId}/history
```

### Request/Response Models

#### Chat Query Request
```typescript
interface ChatQueryRequest {
  query: string;
  userId: string;
  conversationId?: string;
  context?: {
    orderId?: string;
    productId?: string;
    categoryId?: string;
  };
  options?: {
    temperature?: number;
    maxTokens?: number;
    streamResponse?: boolean;
    includeSources?: boolean;
  };
}
```

#### Chat Query Response
```typescript
interface ChatQueryResponse {
  queryId: string;
  conversationId: string;
  response: string;
  sources?: Source[];
  metadata: {
    processingTimeMs: number;
    tokensUsed: number;
    dataSources: string[];
    confidence: number;
  };
  timestamp: string;
}

interface Source {
  type: 'document' | 'database' | 'api';
  title: string;
  excerpt: string;
  url?: string;
  relevanceScore: number;
}
```

### WebSocket Protocol
```typescript
// Client → Server
{
  type: 'chat.message',
  payload: ChatQueryRequest
}

// Server → Client
{
  type: 'chat.response.start',
  payload: { queryId: string }
}

{
  type: 'chat.response.chunk',
  payload: { content: string }
}

{
  type: 'chat.response.complete',
  payload: ChatQueryResponse
}
```

---

## 💾 Data Management

### Data Ingestion Pipeline

#### 1. Blob Storage Ingestion
```csharp
// Monitor Blob Storage for new files
// Extract content using Azure Form Recognizer
// Generate embeddings using Azure OpenAI
// Index in Azure AI Search
```

#### 2. Database Change Tracking
```csharp
// Use SQL Change Tracking or Cosmos Change Feed
// Transform data to searchable format
// Update search index incrementally
```

#### 3. Real-time Updates
```csharp
// Publish events via Azure Service Bus
// Update search index via push API
// Invalidate cache for affected queries
```

### Data Schema Design

#### Unified Search Index Schema
```json
{
  "id": "unique-id",
  "content": "searchable content",
  "contentVector": [/* embedding array */],
  "type": "document|product|order|faq",
  "metadata": {
    "title": "string",
    "category": "string",
    "tags": ["tag1", "tag2"],
    "createdAt": "datetime",
    "updatedAt": "datetime"
  },
  "permissions": ["role1", "role2"],
  "tenantId": "tenant-id"
}
```

### Caching Strategy

- **Query Cache**: Redis cache for frequent queries (TTL: 1 hour)
- **Embedding Cache**: Cache generated embeddings (TTL: 24 hours)
- **Response Cache**: Cache complete responses for identical queries (TTL: 30 min)
- **User Context Cache**: Session-level cache for user data

---

## 🔐 Security & Compliance

### Authentication & Authorization

#### Identity Management
- Azure AD B2C / Entra ID for user authentication
- JWT tokens with refresh token rotation
- Multi-factor authentication (MFA) support
- Role-Based Access Control (RBAC)

#### API Security
- API key authentication for service-to-service
- OAuth 2.0 / OpenID Connect for user flows
- Rate limiting per user/tenant
- IP whitelisting for internal APIs

### Data Protection

#### Encryption
- **At Rest**: Azure Storage encryption (256-bit AES)
- **In Transit**: TLS 1.3 for all connections
- **Database**: Transparent Data Encryption (TDE)
- **Blob Storage**: Customer-managed keys (optional)

#### PII Handling
- Avoid logging sensitive PII (SSN, credit cards, passwords)
- Mask sensitive data in logs and telemetry
- Implement data retention policies
- Support GDPR right to deletion

#### Secrets Management
- Store all secrets in Azure Key Vault
- Use Managed Identity for Azure service access
- Rotate secrets automatically (90-day cycle)
- Never hardcode credentials in code or config

### Compliance

- **GDPR**: Data privacy and user consent
- **PCI-DSS**: Payment card data security (if applicable)
- **SOC 2**: Security and availability controls
- **HIPAA**: Healthcare data protection (if applicable)

---

## 🚀 Performance & Scalability

### Performance Targets

| Metric | Target | Maximum |
|--------|--------|---------|
| Response Time (p95) | < 2s | < 5s |
| Response Time (streaming first chunk) | < 500ms | < 1s |
| Throughput | 1000 req/min | 5000 req/min |
| Concurrent Users | 10,000 | 50,000 |
| Token Efficiency | < 2000 tokens/query | < 4000 tokens/query |
| Cache Hit Rate | > 40% | - |

### Optimization Strategies

#### 1. Query Optimization
- Use embedding cache to avoid redundant embeddings
- Implement query result pagination
- Apply relevance score thresholds early
- Use approximate nearest neighbor (ANN) for vector search

#### 2. Prompt Optimization
- Remove redundant context
- Prioritize most relevant documents
- Implement sliding window for conversation history
- Use token counting before API calls

#### 3. Caching Layers
- CDN for static content
- Redis for query results and embeddings
- Application-level in-memory cache
- Browser cache for client-side

#### 4. Horizontal Scaling
- Stateless API design for easy scaling
- Container orchestration with Kubernetes
- Auto-scaling based on CPU, memory, and queue depth
- Geographic distribution for global users

#### 5. Async Processing
- Use background jobs for long-running tasks
- Implement queue-based processing for indexing
- Support webhook notifications for completion
- Stream responses instead of blocking

---

## 📊 Monitoring & Observability

### Telemetry & Logging

#### Application Insights Integration
```csharp
// Track custom metrics
telemetryClient.TrackMetric("RAG.RetrievalTime", retrievalTime);
telemetryClient.TrackMetric("OpenAI.TokensUsed", tokensUsed);

// Track dependencies
telemetryClient.TrackDependency(
    "Azure AI Search", "SearchQuery", searchQueryId, 
    startTime, duration, success);

// Track custom events
telemetryClient.TrackEvent("Chat.QueryProcessed", properties);
```

#### Structured Logging
```csharp
logger.LogInformation(
    "Query processed: {QueryId}, User: {UserId}, Duration: {DurationMs}ms, Sources: {DataSources}",
    queryId, userId, durationMs, dataSources);
```

### Key Metrics to Monitor

#### Performance Metrics
- Response time (p50, p95, p99)
- Token usage per query
- Cache hit rate
- Database query time
- Search retrieval time
- OpenAI API latency

#### Business Metrics
- Total queries per day/hour
- Unique active users
- Conversation completion rate
- Query satisfaction score
- Cost per query
- Revenue impact (for e-commerce queries)

#### Health Metrics
- API availability (uptime %)
- Error rate by error type
- Rate limit violations
- Queue depth and lag
- Database connection pool utilization

### Alerting Rules

```yaml
Alerts:
  - Name: High Error Rate
    Condition: ErrorRate > 5% for 5 minutes
    Severity: Critical
    
  - Name: Slow Response Time
    Condition: P95ResponseTime > 5s for 10 minutes
    Severity: Warning
    
  - Name: High Token Usage
    Condition: AvgTokensPerQuery > 4000 for 15 minutes
    Severity: Warning
    
  - Name: Low Cache Hit Rate
    Condition: CacheHitRate < 20% for 30 minutes
    Severity: Info
```

### Dashboards

#### Real-time Operations Dashboard
- Current request rate (requests/minute)
- Active connections
- Response time trends
- Error rate by endpoint
- Resource utilization (CPU, memory, network)

#### Business Intelligence Dashboard
- Daily/weekly/monthly query volume
- Most common query types
- User satisfaction trends
- Cost analysis (Azure service costs)
- ROI metrics

---

## 🧪 Testing Strategy

### Test Pyramid

#### 1. Unit Tests (70%)
```csharp
[Fact]
public async Task QueryProcessor_Should_GenerateCorrectPrompt()
{
    // Arrange
    var processor = new QueryProcessor(mockServices);
    var query = "What's the status of order #12345?";
    
    // Act
    var prompt = await processor.BuildPrompt(query, userContext);
    
    // Assert
    prompt.Should().Contain("order #12345");
    prompt.TokenCount.Should().BeLessThan(4000);
}
```

#### 2. Integration Tests (20%)
```csharp
[Fact]
public async Task RAGPipeline_Should_RetrieveRelevantDocuments()
{
    // Arrange
    var pipeline = new RAGPipeline(azureServices);
    
    // Act
    var results = await pipeline.RetrieveContext("product warranty info");
    
    // Assert
    results.Should().NotBeEmpty();
    results.All(r => r.RelevanceScore > 0.7).Should().BeTrue();
}
```

#### 3. End-to-End Tests (10%)
```csharp
[Fact]
public async Task ChatAPI_Should_HandleCompleteUserFlow()
{
    // Test complete flow from query to response
    var response = await client.PostAsync("/api/chat/query", query);
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadAsAsync<ChatQueryResponse>();
    result.Response.Should().NotBeNullOrEmpty();
}
```

### Test Types

#### Functional Tests
- Query processing accuracy
- Intent classification correctness
- Data retrieval completeness
- Response format validation

#### Performance Tests
- Load testing (JMeter, k6, or NBomber)
- Stress testing (peak load scenarios)
- Endurance testing (sustained load)
- Spike testing (sudden traffic increase)

#### Security Tests
- Authentication/authorization validation
- Input validation and sanitization
- SQL injection prevention
- Rate limiting effectiveness
- OWASP Top 10 vulnerability scanning

#### Quality Tests
- Response relevance scoring
- Hallucination detection
- Citation accuracy
- Fact verification against source data

### Test Data Management
- Use anonymized production data
- Generate synthetic test data
- Maintain test data versioning
- Automate test data refresh

---

## 🤖 AI Agent Guidelines

### Code Generation Principles

#### 1. Architecture Standards
✅ **DO**:
- Follow Clean Architecture principles
- Use CQRS pattern with MediatR
- Implement Repository pattern for data access
- Apply Dependency Injection throughout
- Use Result pattern for error handling
- Implement proper separation of concerns

❌ **DON'T**:
- Mix business logic with infrastructure concerns
- Create tight coupling between layers
- Use static classes for business logic
- Bypass dependency injection
- Ignore error handling patterns

#### 2. RAG Implementation Standards
```csharp
// Proper RAG pipeline structure
public class RAGQueryHandler : IRequestHandler<RAGQuery, Result<RAGResponse>>
{
    private readonly IVectorStore _vectorStore;
    private readonly IOpenAIService _openAIService;
    private readonly IContextBuilder _contextBuilder;
    
    public async Task<Result<RAGResponse>> Handle(
        RAGQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Retrieve relevant documents
        var documents = await _vectorStore.SearchAsync(
            request.Query, 
            topK: 10,
            cancellationToken);
            
        // 2. Build optimized context
        var context = await _contextBuilder.BuildAsync(
            documents, 
            request.UserContext,
            cancellationToken);
            
        // 3. Generate response
        var response = await _openAIService.CompleteAsync(
            context,
            cancellationToken);
            
        // 4. Validate and return
        return response.IsSuccess 
            ? Result<RAGResponse>.Success(response.Value)
            : Result<RAGResponse>.Failure(response.Error);
    }
}
```

#### 3. Cost Optimization
- Implement token counting before API calls
- Use cheaper models for simple queries
- Cache embeddings aggressively
- Batch API calls when possible
- Implement early termination for streaming
- Monitor and alert on cost anomalies

#### 4. Latency Optimization
- Execute retrieval operations in parallel
- Use streaming responses
- Implement circuit breakers for external services
- Apply timeout policies
- Pre-warm caches for common queries
- Use connection pooling

#### 5. Quality Safeguards
```csharp
public class HallucinationGuard
{
    public async Task<bool> ValidateResponse(
        string response, 
        IEnumerable<Source> sources)
    {
        // Check if response contains facts not in sources
        // Use fact-checking model or rule-based validation
        // Return confidence score
    }
}
```

### File Structure Standards

```
src/
├── Aiva.Admin.Api.Web/              # API Layer
│   ├── Endpoints/                    # Minimal API endpoints
│   │   ├── Chat/
│   │   │   ├── QueryEndpoint.cs
│   │   │   ├── StreamEndpoint.cs
│   │   │   └── HistoryEndpoint.cs
│   │   └── Admin/
│   ├── Middleware/
│   │   ├── AuthenticationMiddleware.cs
│   │   ├── RateLimitingMiddleware.cs
│   │   └── ExceptionHandlingMiddleware.cs
│   └── Program.cs
│
├── Aiva.Admin.Api.UseCases/         # Application Layer
│   ├── Chat/
│   │   ├── Commands/
│   │   │   ├── ProcessQueryCommand.cs
│   │   │   └── ProcessQueryHandler.cs
│   │   ├── Queries/
│   │   │   ├── GetChatHistoryQuery.cs
│   │   │   └── GetChatHistoryHandler.cs
│   │   └── DTOs/
│   └── RAG/
│       ├── RetrievalService.cs
│       ├── ContextBuilder.cs
│       └── ResponseValidator.cs
│
├── Aiva.Admin.Api.Core/             # Domain Layer
│   ├── ChatAggregate/
│   │   ├── Chat.cs
│   │   ├── Message.cs
│   │   ├── MessageId.cs
│   │   └── Events/
│   ├── Interfaces/
│   │   ├── IVectorStore.cs
│   │   ├── IOpenAIService.cs
│   │   └── IChatRepository.cs
│   └── Specifications/
│
└── Aiva.Admin.Api.Infrastructure/   # Infrastructure Layer
    ├── AI/
    │   ├── AzureOpenAIService.cs
    │   ├── EmbeddingService.cs
    │   └── PromptTemplates/
    ├── Search/
    │   ├── AzureAISearchService.cs
    │   └── VectorStore.cs
    ├── Data/
    │   ├── Repositories/
    │   └── Contexts/
    └── Storage/
        └── BlobStorageService.cs
```

### Documentation Standards

Every generated code file should include:
```csharp
/// <summary>
/// Handles RAG query processing by orchestrating document retrieval,
/// context building, and OpenAI completion.
/// </summary>
/// <remarks>
/// This handler implements the following flow:
/// 1. Retrieve relevant documents using vector search
/// 2. Build optimized context with user history
/// 3. Generate response via Azure OpenAI
/// 4. Validate and format the response
/// </remarks>
public class RAGQueryHandler : IRequestHandler<RAGQuery, Result<RAGResponse>>
{
    // Implementation
}
```

### Code Quality Checklist

Before delivering code, ensure:
- [ ] All dependencies are properly injected
- [ ] Error handling is comprehensive
- [ ] Async/await is used correctly
- [ ] CancellationTokens are passed through
- [ ] Logging is implemented at appropriate levels
- [ ] Unit tests are included
- [ ] XML documentation comments are added
- [ ] SOLID principles are followed
- [ ] No hardcoded values (use configuration)
- [ ] Performance considerations are addressed

---

## 💻 Development Standards

### C# Coding Standards

#### Naming Conventions
```csharp
// Interfaces: IPrefix
public interface IVectorStore { }

// Classes: PascalCase
public class AzureAISearchService { }

// Methods: PascalCase
public async Task<Result> ProcessAsync() { }

// Private fields: _camelCase
private readonly ILogger _logger;

// Parameters & locals: camelCase
public void Method(string userName) { }

// Constants: PascalCase
public const int MaxRetries = 3;
```

#### Async Patterns
```csharp
// Always use async/await for I/O operations
public async Task<Result<Data>> GetDataAsync(
    string id, 
    CancellationToken cancellationToken = default)
{
    try
    {
        var data = await _repository.FindAsync(id, cancellationToken);
        return Result<Data>.Success(data);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve data for {Id}", id);
        return Result<Data>.Failure("Failed to retrieve data");
    }
}
```

#### Error Handling
```csharp
// Use Result pattern instead of exceptions for expected failures
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    
    public static Result<T> Success(T value) => 
        new Result<T> { IsSuccess = true, Value = value };
        
    public static Result<T> Failure(string error) => 
        new Result<T> { IsSuccess = false, Error = error };
}
```

### TypeScript/React Standards (for Frontend)

```typescript
// Use strict TypeScript
interface ChatMessage {
  id: string;
  content: string;
  role: 'user' | 'assistant';
  timestamp: Date;
  sources?: Source[];
}

// Functional components with hooks
export const ChatInterface: React.FC = () => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  
  const sendMessage = useCallback(async (content: string) => {
    // Implementation
  }, []);
  
  return <div>{/* JSX */}</div>;
};
```

### Git Workflow

```bash
# Feature branch naming
feature/chat-streaming-support
bugfix/token-counting-error
hotfix/critical-security-issue

# Commit messages (Conventional Commits)
feat: add streaming support for chat responses
fix: correct token counting in prompt builder
docs: update RAG pipeline documentation
test: add integration tests for vector search
refactor: extract prompt building logic
perf: optimize context assembly

# Branch protection rules
- Require pull request reviews (min 1 reviewer)
- Require status checks to pass
- Require branches to be up to date
- Include administrators in restrictions
```

---

## 🚢 Deployment & CI/CD

### CI/CD Pipeline

#### Build Pipeline (GitHub Actions / Azure DevOps)
```yaml
name: Build and Test

on:
  push:
    branches: [ main, dev ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Run unit tests
      run: dotnet test --no-build --configuration Release --filter Category=Unit
    
    - name: Run integration tests
      run: dotnet test --no-build --configuration Release --filter Category=Integration
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
    
    - name: Build Docker image
      run: docker build -t aiva-admin-api:${{ github.sha }} .
    
    - name: Push to container registry
      run: docker push aiva-admin-api:${{ github.sha }}
```

#### Deployment Stages
```yaml
Stages:
  1. Development (dev)
     - Auto-deploy on commit to dev branch
     - Use dev Azure resources
     - Enable verbose logging
  
  2. Staging (staging)
     - Auto-deploy on commit to main branch
     - Production-like environment
     - Run smoke tests
  
  3. Production (prod)
     - Manual approval required
     - Blue-green deployment
     - Automated rollback on failure
```

### Infrastructure as Code

#### Azure Resources (Bicep)
```bicep
resource openai 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: 'aiva-openai-${environment}'
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: 'aiva-openai-${environment}'
    publicNetworkAccess: 'Enabled'
  }
}

resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: 'aiva-search-${environment}'
  location: location
  sku: {
    name: 'standard'
  }
  properties: {
    replicaCount: 2
    partitionCount: 1
  }
}
```

### Monitoring & Health Checks

```csharp
// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Custom health checks
services.AddHealthChecks()
    .AddCheck<AzureOpenAIHealthCheck>("azure-openai")
    .AddCheck<AzureSearchHealthCheck>("azure-search")
    .AddSqlServer(connectionString, tags: new[] { "ready" })
    .AddRedis(redisConnection, tags: new[] { "ready" });
```

---

## 📚 Agent Responsibilities Summary

As an AI coding agent, you will:

### 1. Architecture & Design
- Design scalable RAG pipeline architectures
- Create detailed system diagrams and flowcharts
- Recommend Azure service configurations
- Optimize data flow and processing patterns
- Design API contracts and interfaces

### 2. Code Generation
- Generate production-ready C# code following Clean Architecture
- Implement RAG components (retrieval, context building, generation)
- Create Azure service integrations (OpenAI, AI Search, Storage)
- Build API endpoints with proper validation and error handling
- Write comprehensive unit and integration tests

### 3. Optimization
- Optimize token usage and prompt engineering
- Improve response latency through caching and parallelization
- Reduce Azure service costs through efficient design
- Enhance search relevance and ranking
- Fine-tune model parameters for better results

### 4. Debugging & Troubleshooting
- Analyze error logs and telemetry data
- Diagnose performance bottlenecks
- Fix bugs in RAG pipeline or API code
- Resolve Azure service integration issues
- Debug production incidents

### 5. Documentation
- Generate comprehensive code documentation
- Create API documentation (OpenAPI/Swagger)
- Write architectural decision records (ADRs)
- Produce deployment guides
- Create troubleshooting runbooks

### 6. Testing
- Write unit tests with high coverage
- Create integration tests for Azure services
- Develop end-to-end test scenarios
- Generate test data and mock services
- Implement performance test scripts

### 7. DevOps & Deployment
- Design CI/CD pipelines
- Create Infrastructure as Code (Bicep/Terraform)
- Configure monitoring and alerting
- Set up health checks and readiness probes
- Implement deployment strategies (blue-green, canary)

---

## 🎯 Success Criteria

### Technical Excellence
- ✅ System achieves <2s p95 response time
- ✅ 99.9% API availability
- ✅ Hallucination rate <2%
- ✅ Search relevance >85% accuracy
- ✅ Token efficiency <2000 tokens/query
- ✅ Cost per query <$0.05

### Code Quality
- ✅ 80%+ unit test coverage
- ✅ All SOLID principles applied
- ✅ Zero critical security vulnerabilities
- ✅ Comprehensive error handling
- ✅ Fully documented codebase

### User Experience
- ✅ Fast, accurate responses
- ✅ Natural conversation flow
- ✅ Proper citations and sources
- ✅ Graceful error handling
- ✅ Mobile-responsive design

### Business Impact
- ✅ Reduce customer support tickets by 30%
- ✅ Improve customer satisfaction (CSAT >4.5/5)
- ✅ Increase operational efficiency
- ✅ Enable self-service capabilities
- ✅ Generate actionable insights from interactions

---

## 📖 References & Resources

### Azure Documentation
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure AI Search](https://learn.microsoft.com/azure/search/)
- [Azure Cosmos DB](https://learn.microsoft.com/azure/cosmos-db/)
- [Azure Blob Storage](https://learn.microsoft.com/azure/storage/blobs/)

### Best Practices
- [RAG Patterns and Anti-Patterns](https://learn.microsoft.com/azure/architecture/ai-ml/guide/rag-patterns)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [API Design Guidelines](https://learn.microsoft.com/azure/architecture/best-practices/api-design)

### Tools & Libraries
- MediatR for CQRS
- Ardalis.Result for Result pattern
- Ardalis.Specification for Repository pattern
- Serilog for structured logging
- Polly for resilience and transient fault handling

---

**Last Updated**: November 2025  
**Version**: 2.0  
**Status**: Active Development
