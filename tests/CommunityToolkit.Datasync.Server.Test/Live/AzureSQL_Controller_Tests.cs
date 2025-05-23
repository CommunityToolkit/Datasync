// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.Server.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.Test.Live;

[ExcludeFromCodeCoverage]
[Collection("LiveTestsCollection")]
public class AzureSQL_Controller_Tests(MsSqlDatabaseFixture fixture, ITestOutputHelper output) : LiveControllerTests<AzureSqlEntityMovie>, IClassFixture<MsSqlDatabaseFixture>, IAsyncLifetime
{
    #region Setup
    private readonly Random random = new();
    private List<AzureSqlEntityMovie> movies = [];

    public async Task InitializeAsync()
    {
        Context = await AzureSqlDbContext.CreateContextAsync(fixture.ConnectionString, output);
        this.movies = await Context.Movies.AsNoTracking().ToListAsync();
    }

    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }
    }

    private AzureSqlDbContext Context { get; set; }

    protected override string DriverName { get; } = "AzureSQL";

    protected override bool CanRunLiveTests() => true;

    protected override Task<AzureSqlEntityMovie> GetEntityAsync(string id)
        => Task.FromResult(Context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(Context.Movies.Count());

    protected override Task<IRepository<AzureSqlEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<AzureSqlEntityMovie>>(new EntityTableRepository<AzureSqlEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    #endregion
}
