// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AutoMapper;
using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Server.Automapper.Test.Helpers;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.Automapper.Test;

[ExcludeFromCodeCoverage]
public class MappedTableRepository_Tests(ITestOutputHelper output) : RepositoryTests<MovieDto>()
{
    #region Setup
    private readonly Random random = new();
    private SqliteDbContext context;
    private EntityTableRepository<SqliteEntityMovie> innerRepository;
    private readonly IMapper mapper = new MapperConfiguration(c => c.AddProfile(new MapperProfile())).CreateMapper();
    private MappedTableRepository<SqliteEntityMovie, MovieDto> repository;
    private List<MovieDto> movies;

    protected override Task<MovieDto> GetEntityAsync(string id)
        => Task.FromResult(this.mapper.Map<MovieDto>(this.context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id)));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(this.context.Movies.Count());

    protected override Task<IRepository<MovieDto>> GetPopulatedRepositoryAsync()
    {
        this.context = SqliteDbContext.CreateContext(output);
        this.innerRepository = new EntityTableRepository<SqliteEntityMovie>(this.context);
        this.repository = new MappedTableRepository<SqliteEntityMovie, MovieDto>(this.mapper, this.innerRepository);
        this.movies = this.context.Movies.AsNoTracking().ToList().ConvertAll(m => this.mapper.Map<MovieDto>(m));
        return Task.FromResult<IRepository<MovieDto>>(this.repository);
    }

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());
    #endregion
}
