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

        // Create the indices required for all the tests.
        //List<CreateIndexModel<MongoDBMovie>> indices = [];
        //string[] props = ["BestPictureWinner", "Duration", "Rating", "ReleaseDate", "Title", "Year", "UpdatedAt", "Deleted"];
        //foreach (string prop in props)
        //{
        //    indices.AddRange(GetCompoundIndexDefinitions(prop));
        //}

        //indices.AddRange(GetCompoundIndexDefinitions("UpdatedAt", "Deleted", includeId: false));
        //indices.AddRange(GetCompoundIndexDefinitions("Title", "Year"));
        //indices.AddRange(GetCompoundIndexDefinitions("Year", "Title"));
        //await Movies.Indexes.CreateManyAsync(indices);

        // Now populate the database with the test data, after the indices are defined.
        foreach (MongoDBMovie movie in TestData.Movies.OfType<MongoDBMovie>())
        {
            movie.UpdatedAt = DateTimeOffset.UtcNow;
            movie.Version = Guid.NewGuid().ToByteArray();
            InsertOneOptions options = new();
            await Movies.InsertOneAsync(movie, options);
        }
    }

    private static IEnumerable<CreateIndexModel<MongoDBMovie>> GetCompoundIndexDefinitions(string field)
    {
        return [
            new CreateIndexModel<MongoDBMovie>(Builders<MongoDBMovie>.IndexKeys.Combine(
                Builders<MongoDBMovie>.IndexKeys.Ascending(field),
                Builders<MongoDBMovie>.IndexKeys.Ascending("_id")
            )),
            new CreateIndexModel<MongoDBMovie>(Builders<MongoDBMovie>.IndexKeys.Combine(
                Builders<MongoDBMovie>.IndexKeys.Descending(field),
                Builders<MongoDBMovie>.IndexKeys.Ascending("_id")
            ))
        ];
    }

    private static IEnumerable<CreateIndexModel<MongoDBMovie>> GetCompoundIndexDefinitions(string field1, string field2, bool includeId = true)
    {
        if (includeId)
        {
            return [
                new CreateIndexModel<MongoDBMovie>(Builders<MongoDBMovie>.IndexKeys.Combine(
                    Builders<MongoDBMovie>.IndexKeys.Ascending(field1),
                    Builders<MongoDBMovie>.IndexKeys.Ascending(field2),
                    Builders<MongoDBMovie>.IndexKeys.Ascending("_id")
                )),
                new CreateIndexModel<MongoDBMovie>(Builders<MongoDBMovie>.IndexKeys.Combine(
                    Builders<MongoDBMovie>.IndexKeys.Ascending(field1),
                    Builders<MongoDBMovie>.IndexKeys.Descending(field2),
                    Builders<MongoDBMovie>.IndexKeys.Ascending("_id")
                )),
                new CreateIndexModel<MongoDBMovie>(Builders<MongoDBMovie>.IndexKeys.Combine(
                    Builders<MongoDBMovie>.IndexKeys.Descending(field1),
                    Builders<MongoDBMovie>.IndexKeys.Ascending(field2),
                    Builders<MongoDBMovie>.IndexKeys.Ascending("_id")
                )),
                new CreateIndexModel<MongoDBMovie>(Builders<MongoDBMovie>.IndexKeys.Combine(
                    Builders<MongoDBMovie>.IndexKeys.Descending(field1),
                    Builders<MongoDBMovie>.IndexKeys.Descending(field2),
                    Builders<MongoDBMovie>.IndexKeys.Ascending("_id")
                )),
            ];
        }
        else
        {
            return [
                new CreateIndexModel<MongoDBMovie>(Builders<MongoDBMovie>.IndexKeys.Combine(
                    Builders<MongoDBMovie>.IndexKeys.Ascending(field1),
                    Builders<MongoDBMovie>.IndexKeys.Ascending(field2)
                )),
                new CreateIndexModel<MongoDBMovie>(Builders<MongoDBMovie>.IndexKeys.Combine(
                    Builders<MongoDBMovie>.IndexKeys.Ascending(field1),
                    Builders<MongoDBMovie>.IndexKeys.Descending(field2)
                )),
                new CreateIndexModel<MongoDBMovie>(Builders<MongoDBMovie>.IndexKeys.Combine(
                    Builders<MongoDBMovie>.IndexKeys.Descending(field1),
                    Builders<MongoDBMovie>.IndexKeys.Ascending(field2)
                )),
                new CreateIndexModel<MongoDBMovie>(Builders<MongoDBMovie>.IndexKeys.Combine(
                    Builders<MongoDBMovie>.IndexKeys.Descending(field1),
                    Builders<MongoDBMovie>.IndexKeys.Descending(field2)
                )),
            ];
        }
    }
}
