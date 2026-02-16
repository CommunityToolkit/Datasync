// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using AutoMapper;
using CommunityToolkit.Datasync.Server.Automapper.Test.Helpers;
using CommunityToolkit.Datasync.Server.EntityFrameworkCore;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Xunit;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Server.Automapper.Test;

[ExcludeFromCodeCoverage]
public class MappedTableRepository_Tests : RepositoryTests<MovieDto>, IDisposable
{
    #region Setup
    private readonly ITestOutputHelper output;
    private readonly Random random = new();
    private SqliteDbContext context;
    private EntityTableRepository<SqliteEntityMovie> innerRepository;
    private readonly ILoggerFactory loggerFactory;
    private readonly IMapper mapper;
    private MappedTableRepository<SqliteEntityMovie, MovieDto> repository;
    private List<MovieDto> movies;
    private bool _disposedValue;

    public MappedTableRepository_Tests(ITestOutputHelper output)
    {
        this.output = output;
        this.loggerFactory = new LoggerFactory([ new XunitLoggerProvider(this.output) ]);
        MapperConfiguration config = new(c => c.AddProfile(new MapperProfile()), this.loggerFactory);
        this.mapper = config.CreateMapper();
    }

    protected override Task<MovieDto> GetEntityAsync(string id)
        => Task.FromResult(this.mapper.Map<MovieDto>(this.context.Movies.AsNoTracking().SingleOrDefault(m => m.Id == id)));

    protected override Task<int> GetEntityCountAsync()
        => Task.FromResult(this.context.Movies.Count());

    protected override Task<IRepository<MovieDto>> GetPopulatedRepositoryAsync()
    {
        this.context = SqliteDbContext.CreateContext(this.output);
        this.innerRepository = new EntityTableRepository<SqliteEntityMovie>(this.context);
        this.repository = new MappedTableRepository<SqliteEntityMovie, MovieDto>(this.mapper, this.innerRepository);
        this.movies = this.context.Movies.AsNoTracking().ToList().ConvertAll(m => this.mapper.Map<MovieDto>(m));
        return Task.FromResult<IRepository<MovieDto>>(this.repository);
    }

    protected override Task<string> GetRandomEntityIdAsync(bool exists)
        => Task.FromResult(exists ? this.movies[this.random.Next(this.movies.Count)].Id : Guid.NewGuid().ToString());

    protected virtual void Dispose(bool disposing)
    {
        if (!this._disposedValue)
        {
            if (disposing)
            {
                this.context.Dispose();
                this.loggerFactory.Dispose();
            }

            this._disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
