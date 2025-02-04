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
public class PgSQL_Controller_Tests(DatabaseFixture fixture, ITestOutputHelper output) : LiveControllerTests<PgEntityMovie>, IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private List<PgEntityMovie> movies = [];

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(ConnectionStrings.PgSql))
        {
            // Note: we don't clear entities on every run to speed up the test runs.  This can only be done because
            // the tests are read-only (associated with the query and get capabilities).  If the test being run writes
            // to the database then change clearEntities to true.
            output.WriteLine($"PgIsInitialized = {fixture.PgIsInitialized}");
            Context = await PgDbContext.CreateContextAsync(ConnectionStrings.PgSql, output, clearEntities: !fixture.PgIsInitialized);
            this.movies = Context.Movies.AsNoTracking().ToList();
            fixture.PgIsInitialized = true;
        }
    }

    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }
    }

    private PgDbContext Context { get; set; }

    protected override string DriverName { get; } = "PgSQL";

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(ConnectionStrings.PgSql);

    protected override async Task<PgEntityMovie> GetEntityAsync(string id)
        => await Context.Movies.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);

    protected override async Task<int> GetEntityCountAsync()
        => await Context.Movies.CountAsync();

    protected override Task<IRepository<PgEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<PgEntityMovie>>(new EntityTableRepository<PgEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    #endregion
}
