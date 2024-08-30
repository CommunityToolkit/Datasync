// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.InMemory;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using CommunityToolkit.Datasync.TestCommon.TestData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Spatial;
using System.Linq.Expressions;

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

[ExcludeFromCodeCoverage]
public abstract class ServiceTest(ServiceApplicationFactory factory)
{
    protected readonly HttpClient client = factory.CreateClient();

    protected DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;

    internal IntegrationDbContext GetOfflineContext(bool useRealFile = false, bool enableLogging = false)
    {
        string filename = null;
        string connectionString = "Data Source=:memory:";
        if (useRealFile)
        {
            filename = Path.GetTempFileName();
            SqliteConnectionStringBuilder builder = new()
            {
                DataSource = filename,
                Mode = SqliteOpenMode.ReadWriteCreate
            };
            connectionString = builder.ConnectionString;
        }

        SqliteConnection connection = new(connectionString);
        connection.Open();

        DbContextOptionsBuilder<IntegrationDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlite(connection);
        if (enableLogging)
        {
            optionsBuilder.LogTo(Console.WriteLine);
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }

        IntegrationDbContext context = new(optionsBuilder.Options) 
        {
            Factory = factory,
            Filename = filename,
            Connection = connection
        };

        context.Database.EnsureCreated();
        return context;
    }

    internal DatasyncServiceClient<ClientMovie> GetMovieClient()
        => new(new Uri($"/{factory.MovieEndpoint}", UriKind.Relative), this.client);

    internal DatasyncServiceClient<ClientMovie> GetSoftDeletedMovieClient()
    => new(new Uri($"/{factory.SoftDeletedMovieEndpoint}", UriKind.Relative), this.client);

    internal DatasyncServiceClient<ClientKitchenSink> GetKitchenSinkClient()
        => new(new Uri($"/{factory.KitchenSinkEndpoint}", UriKind.Relative), this.client);

    internal int Count<TEntity>() where TEntity : InMemoryTableData
        => factory.Count<TEntity>();

    internal InMemoryMovie GetRandomMovie()
        => factory.GetRandomMovie();

    internal TEntity GetServerEntityById<TEntity>(string id) where TEntity : InMemoryTableData
        => factory.GetServerEntityById<TEntity>(id);

    internal void ResetInMemoryMovies()
        => factory.ResetInMemoryMovies();

    protected void SeedKitchenSinkWithCountryData()
    {
        factory.RunWithRepository<InMemoryKitchenSink>(repository =>
        {
            repository.Clear();
            foreach (Country countryRecord in CountryData.GetCountries())
            {
                InMemoryKitchenSink model = new()
                {
                    Id = countryRecord.IsoCode,
                    Version = Guid.NewGuid().ToByteArray(),
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Deleted = false,
                    PointValue = GeographyPoint.Create(countryRecord.Latitude, countryRecord.Longitude),
                    StringValue = countryRecord.CountryName
                };
                repository.StoreEntity(model);
            }
        });
    }

    protected void SeedKitchenSinkWithDateTimeData()
    {
        factory.RunWithRepository<InMemoryKitchenSink>(repository =>
        {
            repository.Clear();
            DateOnly SourceDate = new(2022, 1, 1);
            for (int i = 0; i < 365; i++)
            {
                DateOnly date = SourceDate.AddDays(i);
                InMemoryKitchenSink model = new()
                {
                    Id = string.Format("id-{0:000}", i),
                    Version = Guid.NewGuid().ToByteArray(),
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Deleted = false,
                    DateOnlyValue = date,
                    TimeOnlyValue = new TimeOnly(date.Month, date.Day)
                };
                repository.StoreEntity(model);
            }
        });
    }

    internal void SoftDelete<TEntity>(TEntity entity, bool deleted = true) where TEntity : InMemoryTableData
        => factory.SoftDelete<TEntity>(entity, deleted);

    internal void SoftDelete<TEntity>(Expression<Func<TEntity, bool>> expression, bool deleted = true) where TEntity : InMemoryTableData
        => factory.SoftDelete<TEntity>(expression, deleted);
}
