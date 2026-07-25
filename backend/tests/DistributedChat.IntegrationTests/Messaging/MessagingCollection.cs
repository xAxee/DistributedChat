using DistributedChat.IntegrationTests.Persistence;

namespace DistributedChat.IntegrationTests.Messaging;

[CollectionDefinition(TestCollections.Messaging, DisableParallelization = true)]
public sealed class MessagingTestCollectionDefinition :
    ICollectionFixture<PostgreSqlFixture>,
    ICollectionFixture<RabbitMqFixture>;
