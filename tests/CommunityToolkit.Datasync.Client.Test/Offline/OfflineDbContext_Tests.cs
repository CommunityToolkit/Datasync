// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0051 // Remove unused private members

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class OfflineDbContext_Tests
{
    #region Helpers
    /// <summary>
    /// Creates a version of the TestDbContext backed by SQLite.
    /// </summary>
    /// <returns></returns>
    private static TestDbContext CreateContext()
    {
        SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();
        DbContextOptionsBuilder<TestDbContext> optionsBuilder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection);
        TestDbContext context = new(optionsBuilder.Options) { Connection = connection };
        return context;
    }
    #endregion

    [Fact]
    public void InitializeDatasyncEntityMap__OptionsBuilder_Works()
    {
        Dictionary<string, Type> expected = new()
        {
            { typeof(ClientMovie).FullName, typeof(ClientMovie) },
            { typeof(Entity3).FullName, typeof(Entity3) }
        };
        TestDbContext sut = CreateContext();
        sut.InitializeDatasyncEntityMap();
        sut.DatasyncEntityMap.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void InitializeDatasyncEntityMap__OnConfiguring_Works()
    {
        Dictionary<string, Type> expected = new()
        {
            { typeof(ClientMovie).FullName, typeof(ClientMovie) },
            { typeof(Entity3).FullName, typeof(Entity3) }
        };
        TestDbContext sut = new();
        sut.InitializeDatasyncEntityMap();
        sut.DatasyncEntityMap.Should().BeEquivalentTo(expected);
    }

    #region IDisposable
    [Fact]
    public void Dispose_Works()
    {
        TestDbContext sut = CreateContext();
        sut.Dispose();
        sut.Dispose();
        sut._disposedValue.Should().BeTrue();

        Action act = () => sut.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_bool_Works()
    {
        TestDbContext sut = CreateContext();
        sut.TestDispose(false); // Doesn't dispose the underlying thing
        sut._disposedValue.Should().BeTrue();

        Action act = () => sut.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }
    #endregion

    /// <summary>
    /// This db context has two synchronizable entities - Movies and KitchenSinks.
    /// </summary>
    public class TestDbContext : OfflineDbContext
    {
        public TestDbContext() : base()
        {
        }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        { 
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

    public record Entity1(string Id, DateTimeOffset? UpdatedAt, string Version);
    public record Entity2(string Id, DateTimeOffset? UpdatedAt, string Version);
    public record Entity3(string Id, DateTimeOffset? UpdatedAt, string Version);
    public record Entity4(string Id, DateTimeOffset? UpdatedAt, string Version);
}
