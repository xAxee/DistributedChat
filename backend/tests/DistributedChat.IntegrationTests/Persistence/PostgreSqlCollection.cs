namespace DistributedChat.IntegrationTests.Persistence;

[CollectionDefinition(TestCollections.PostgreSql, DisableParallelization = true)]
public sealed class PostgreSqlTestCollectionDefinition : ICollectionFixture<PostgreSqlFixture>;
