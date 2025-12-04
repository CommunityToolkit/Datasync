// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Abstractions.Json;
using CommunityToolkit.Datasync.Server.CosmosDb.Test.Models;
using CommunityToolkit.Datasync.Server.CosmosDb.Test.Options;
using CommunityToolkit.Datasync.TestCommon;
using AwesomeAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Server.CosmosDb.Test;

[ExcludeFromCodeCoverage]
public class PackedKeyRepository_Tests : RepositoryTests<CosmosDbMovie>, IDisposable, IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private readonly string connectionString = Environment.GetEnvironmentVariable("DATASYNC_COSMOS_CONNECTIONSTRING");
    private readonly List<CosmosDbMovie> movies = [];

    private CosmosClient _client;
    private Container _container;
    private CosmosTableRepository<CosmosDbMovie> _repository;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._client?.Dispose();
        }
    }

    override protected bool CanRunLiveTests() => !string.IsNullOrEmpty(this.connectionString);
    protected override async Task<CosmosDbMovie> GetEntityAsync(string id)
    {
        try
        {
            string[] parts = id.Split(':');

            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid ID format");
            }

            if (!int.TryParse(parts[1], out int year))
            {
                throw new ArgumentException("Invalid ID Part");
            }

            return await this._container.ReadItemAsync<CosmosDbMovie>(id, new PartitionKey(year));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    protected override async Task<int> GetEntityCountAsync()
    {
        return await this._container.GetItemLinqQueryable<CosmosDbMovie>().CountAsync();
    }

    protected override Task<IRepository<CosmosDbMovie>> GetPopulatedRepositoryAsync()
    {
        return Task.FromResult<IRepository<CosmosDbMovie>>(this._repository);
    }

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
    {
        return Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : $"{Guid.NewGuid()}:2018");
    }

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(this.connectionString))
        {
            this._client = new CosmosClient(
                this.connectionString,
                new CosmosClientOptions()
                {
                    UseSystemTextJsonSerializerWithOptions = new(JsonSerializerDefaults.Web)
                    {
                        AllowTrailingCommas = true,
                        Converters =
                        {
                            new JsonStringEnumConverter(),
                            new DateTimeOffsetConverter(),
                            new DateTimeConverter(),
                            new TimeOnlyConverter(),
                            new SpatialGeoJsonConverter()
                        },
                        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                        IgnoreReadOnlyFields = true,
                        IgnoreReadOnlyProperties = true,
                        IncludeFields = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString,
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    }
                });

            Database database = await this._client.CreateDatabaseIfNotExistsAsync("Movies");

            this._container = await database.CreateContainerIfNotExistsAsync(new ContainerProperties("Movies", "/year")
            {
                IndexingPolicy = new IndexingPolicy()
                {
                    CompositeIndexes =
                    {
                        new Collection<CompositePath>()
                        {
                            new() { Path = "/updatedAt", Order = CompositePathSortOrder.Ascending },
                            new() { Path = "/id", Order = CompositePathSortOrder.Ascending }
                        },
                        new Collection<CompositePath>()
                        {
                            new() { Path = "/releaseDate", Order = CompositePathSortOrder.Ascending },
                            new() { Path = "/id", Order = CompositePathSortOrder.Ascending }
                        }
                    }
                }
            });

            foreach (CosmosDbMovie movie in TestData.Movies.OfType<CosmosDbMovie>())
            {
                movie.Id = $"{Guid.NewGuid()}:{movie.Year}";
                movie.UpdatedAt = DateTimeOffset.UtcNow;
                movie.Version = Guid.NewGuid().ToByteArray();
                _ = await this._container.CreateItemAsync(movie, new PartitionKey(movie.Year));
                this.movies.Add(movie);
            }

            this._repository = new CosmosTableRepository<CosmosDbMovie>(
                this._client,
                new PackedKeyOptions("Movies", "Movies")
                );
        }
    }

    public async Task DisposeAsync()
    {
        if (this._client != null)
        {
            try
            {
                await this._client.GetDatabase("Movies").DeleteAsync();
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Ignore
            }
        }
    }
    #endregion

    [SkippableTheory]
    [InlineData("BadId")]
    [InlineData("12345-12345")]
    public async Task ReadAsync_Throws_OnMalformedId(string id)
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosDbMovie> Repository = await GetPopulatedRepositoryAsync();
        Func<Task> act = async () => _ = await Repository.ReadAsync(id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400);
    }

    [SkippableTheory]
    [InlineData("BadId")]
    [InlineData("12345-12345")]
    public async Task DeleteAsync_Throws_OnMalformedIds(string id)
    {
        Skip.IfNot(CanRunLiveTests());

        IRepository<CosmosDbMovie> Repository = await GetPopulatedRepositoryAsync();
        Func<Task> act = async () => await Repository.DeleteAsync(id);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(400);
        (await GetEntityCountAsync()).Should().Be(TestData.Movies.Count<CosmosDbMovie>());
    }
}
