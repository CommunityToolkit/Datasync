// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test;
using LiteDB;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Server.LiteDb.Test;

[ExcludeFromCodeCoverage]
public class LiteDbRepository_Tests : RepositoryTests<LiteDbMovie>, IDisposable
{
    #region Setup
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

        foreach (LiteDbMovie movie in TestData.Movies.OfType<LiteDbMovie>())
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
    {
        Random random = new();
        return Task.FromResult(exists ? this.movies[random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    }

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
