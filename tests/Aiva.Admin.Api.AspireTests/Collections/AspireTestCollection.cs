namespace Aiva.Admin.Api.AspireTests.Collections;

/// <summary>
/// Collection definition for Aspire integration tests.
/// All tests in this collection share the same AspireAppFixture instance,
/// which means the distributed application is started once and reused across all tests.
/// This improves test performance significantly.
/// </summary>
[CollectionDefinition(Name)]
public class AspireTestCollection : ICollectionFixture<AspireAppFixture>
{
  public const string Name = "Aspire Integration Tests";
}


