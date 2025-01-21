// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Abstractions.Http;
using CommunityToolkit.Datasync.Server.InMemory;
using CommunityToolkit.Datasync.Server.LiteDb;
using CommunityToolkit.Datasync.Server.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using LiteDB;

namespace CommunityToolkit.Datasync.Server.Test.Live;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class LiteDb_Controller_Tests : LiveControllerTests<LiteDbMovie>, IDisposable
{
    #region Setup
    private readonly Random random = new();
    private string dbFilename;
    private LiteDatabase database;
    private ILiteCollection<LiteDbMovie> collection;
    private LiteDbRepository<LiteDbMovie> repository;
    private readonly List<LiteDbMovie> movies = [];

    protected override Task<LiteDbMovie> GetEntityAsync(string id)
        => Task.FromResult(this.collection.FindById(id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(this.collection.Count());

    protected override Task<IRepository<LiteDbMovie>> GetPopulatedRepositoryAsync()
    {
        this.dbFilename = Path.GetTempFileName();
        this.database = new LiteDatabase($"Filename={this.dbFilename};Connection=direct;InitialSize=0");
        this.collection = this.database.GetCollection<LiteDbMovie>("litedbmovies");

        foreach (LiteDbMovie movie in TestCommon.TestData.Movies.OfType<LiteDbMovie>())
        {
            movie.UpdatedAt = DateTimeOffset.Now;
            movie.Version = Guid.NewGuid().ToByteArray();
            this.collection.Insert(movie);
            this.movies.Add(movie);
        }

        this.repository = new LiteDbRepository<LiteDbMovie>(this.database);
        return Task.FromResult<IRepository<LiteDbMovie>>(this.repository);
    }

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.database?.Dispose();
            if (!string.IsNullOrEmpty(this.dbFilename))
            {
                File.Delete(this.dbFilename);
            }
        }
    }
    #endregion
}
