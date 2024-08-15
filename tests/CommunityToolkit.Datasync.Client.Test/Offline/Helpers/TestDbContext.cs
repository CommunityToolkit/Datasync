// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0051 // Remove unused private members

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Mocks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.Test.Offline.Helpers;

/// <summary>
/// This db context has two synchronizable entities - Movies and KitchenSinks.
/// </summary>
[ExcludeFromCodeCoverage]
public class TestDbContext : OfflineDbContext
{
    public TestDbContext() : base()
    {
    }

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        HttpClientOptions clientOptions = new()
        {
            Endpoint = new Uri("https://test.zumo.net"),
            HttpPipeline = [ Handler ]
        };
        optionsBuilder
            .UseHttpClientOptions(clientOptions)
            .Entity<ClientMovie>(c =>
            {
                c.ClientName = "movies";
                c.Endpoint = new Uri("/tables/movies", UriKind.Relative);
            });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            Connection = new("Data Source=:memory:");
            Connection.Open();
            optionsBuilder.UseSqlite();
        }

        base.OnConfiguring(optionsBuilder);
    }

    internal SqliteConnection Connection { get; set; }

    internal MockDelegatingHandler Handler { get; set; } = new();

    public DbSet<ClientMovie> Movies => Set<ClientMovie>();

    [DoNotSynchronize]
    public DbSet<Entity1> EntitySet1 => Set<Entity1>();

    private DbSet<Entity2> EntitySet2 => Set<Entity2>();
    public DbSet<Entity3> EntitySet3 => Set<Entity3>();
    internal DbSet<Entity4> EntitySet4 { get; set; }
    public int EntitySet5 { get; set; }
    public static int EntitySet6 { get; set; }

    internal void TestDispose(bool disposing)
    {
        if (disposing)
        {
            Connection.Dispose();
        }

        base.Dispose(disposing);
    }
}

[ExcludeFromCodeCoverage]
public record Entity1(string Id, DateTimeOffset? UpdatedAt, string Version);

[ExcludeFromCodeCoverage]
public record Entity2(string Id, DateTimeOffset? UpdatedAt, string Version);

[ExcludeFromCodeCoverage]
public record Entity3(string Id, DateTimeOffset? UpdatedAt, string Version);

[ExcludeFromCodeCoverage]
public record Entity4(string Id, DateTimeOffset? UpdatedAt, string Version);

