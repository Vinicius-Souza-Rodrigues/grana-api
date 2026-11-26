namespace IntegrationTests.Shared;

[CollectionDefinition(Nome)]
public sealed class ApiCollection : ICollectionFixture<GranaApiFactory>
{
    public const string Nome = "Api";
}
