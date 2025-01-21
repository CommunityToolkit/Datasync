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
public class AzureSQL_Controller_Tests : LiveControllerTests<AzureSqlEntityMovie>
{
    #region Setup
    private readonly DatabaseFixture _fixture;
    private readonly Random random = new();
    private readonly string connectionString;
    private readonly List<AzureSqlEntityMovie> movies;

    public AzureSQL_Controller_Tests(DatabaseFixture fixture, ITestOutputHelper output) : base()
    {
        this._fixture = fixture;
        this.connectionString = Environment.GetEnvironmentVariable("DATASYNC_AZSQL_CONNECTIONSTRING");
        if (!string.IsNullOrEmpty(this.connectionString))
        {
            output.WriteLine($"AzureSqlIsInitialized = {this._fixture.AzureSqlIsInitialized}");
            Context = AzureSqlDbContext.CreateContext(this.connectionString, output, clearEntities: !this._fixture.AzureSqlIsInitialized);
            this.movies = [.. Context.Movies.AsNoTracking()];
            this._fixture.AzureSqlIsInitialized = true;
        }
    }

    private AzureSqlDbContext Context { get; set; }

    protected override string DriverName { get; } = "AzureSQL";

    protected override bool CanRunLiveTests() => !string.IsNullOrEmpty(this.connectionString);

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
