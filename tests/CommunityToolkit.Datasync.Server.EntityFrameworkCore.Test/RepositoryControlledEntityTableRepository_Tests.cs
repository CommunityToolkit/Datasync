// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.EntityFrameworkCore.Test;

[ExcludeFromCodeCoverage]
public class RepositoryControlledEntityTableRepository_Tests : RepositoryTests<RepositoryControlledEntityMovie>
{
    #region Setup
    private readonly Random random = new();
    private readonly List<RepositoryControlledEntityMovie> movies;
    private readonly Lazy<RepositoryControlledDbContext> lazyContext;

    public RepositoryControlledEntityTableRepository_Tests(ITestOutputHelper output)
    {
        this.lazyContext = new(() => RepositoryControlledDbContext.CreateContext(output));
        this.movies = [.. Context.Movies.AsNoTracking()];
    }

    private RepositoryControlledDbContext Context { get => this.lazyContext.Value; }

    protected override Task<RepositoryControlledEntityMovie> GetEntityAsync(string id)
        => Task.FromResult(Context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(Context.Movies.Count());

    protected override Task<IRepository<RepositoryControlledEntityMovie>> GetPopulatedRepositoryAsync()
        => Task.FromResult<IRepository<RepositoryControlledEntityMovie>>(new EntityTableRepository<RepositoryControlledEntityMovie>(Context));

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(Context.Movies.Count())].Id : Guid.NewGuid().ToString());
    #endregion
}
