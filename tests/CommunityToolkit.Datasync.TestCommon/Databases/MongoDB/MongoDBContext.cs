// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.TestCommon.Databases;

[ExcludeFromCodeCoverage]
public class MongoDBContext(MongoClient client, IMongoDatabase database) : IAsyncDisposable
{
    public static async Task<MongoDBContext> CreateContextAsync(string connectionString, ITestOutputHelper output, bool clearEntities = true)
    {
        MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
        if (output is not null)
        {
            settings.ClusterConfigurator = cb => cb.Subscribe<CommandStartedEvent>(e => output.WriteLine($"{e.CommandName} - {e.Command.ToJson()}"));
        }

        MongoClient client = new(settings);
        IMongoDatabase database = client.GetDatabase("unittests");
        MongoDBContext context = new(client, database);

        await context.InitializeDatabaseAsync(clearEntities);
        await context.PopulateDatabaseAsync();

        return context;
    }

    public ValueTask DisposeAsync()
    {
        client.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public MongoClient Client => client;
    public IMongoDatabase Database => database;
    public IMongoCollection<MongoDBMovie> Movies => database.GetCollection<MongoDBMovie>("movies");

    public async Task InitializeDatabaseAsync(bool clearEntities)
    {
        if (clearEntities)
        {
            FilterDefinition<MongoDBMovie> filter = Builders<MongoDBMovie>.Filter.Empty;
            await Movies.DeleteManyAsync(filter);
        }
    }

    public async Task PopulateDatabaseAsync()
    {
        FilterDefinition<MongoDBMovie> filter = Builders<MongoDBMovie>.Filter.Empty;
        bool hasEntities = await Movies.CountDocumentsAsync(filter) > 0;
        if (hasEntities)
        {
            return;
        }

        foreach (MongoDBMovie movie in TestData.Movies.OfType<MongoDBMovie>())
        {
            movie.UpdatedAt = DateTimeOffset.UtcNow;
            movie.Version = Guid.NewGuid().ToByteArray();
            InsertOneOptions options = new();
            await Movies.InsertOneAsync(movie, options);
        }
    }
}
