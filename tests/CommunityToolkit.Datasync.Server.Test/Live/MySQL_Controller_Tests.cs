// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.Server.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.Test.Live;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class MySQL_Controller_Tests : LiveControllerTests<MysqlEntityMovie>
{
    #region Setup
    private readonly DatabaseFixture _fixture;
    private readonly Random random = new();
    private readonly string connectionString;
    private readonly List<MysqlEntityMovie> movies;

    public MySQL_Controller_Tests(DatabaseFixture fixture, ITestOutputHelper output) : base()
    {
        this._fixture = fixture;
        this.connectionString = Environment.GetEnvironmentVariable("DATASYNC_MYSQL_CONNECTIONSTRING");
        if (!string.IsNullOrEmpty(this.connectionString))
        {
            output.WriteLine($"MysqlIsInitialized = {this._fixture.MysqlIsInitialized}");
            Context = MysqlDbContext.CreateContext(this.connectionString, output, clearEntities: !this._fixture.MysqlIsInitialized);
            this.movies = Context.Movies.AsNoTracking().ToList();
            this._fixture.MysqlIsInitialized = true;
        }
    }

    private MysqlDbContext Context { get; set; }

    protected override string DriverName { get; } = "PgSQL";

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(this.connectionString);

    protected override Task<MysqlEntityMovie> GetEntityAsync(string id)
        => Task.FromResult(Context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(Context.Movies.Count());

    protected override Task<IRepository<MysqlEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<MysqlEntityMovie>>(new EntityTableRepository<MysqlEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    #endregion
}
