// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.EncryptedSqlite.Test.Helpers;

/// <summary>
/// Base class for the encrypted SQLite tests.  Provides a unique on-disk database file (with connection pooling
/// disabled so the file is released between contexts) and convenience seed/read/assert helpers.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class EncryptedSqliteTestBase : IDisposable
{
    protected EncryptedSqliteTestBase()
    {
        DatabasePath = Path.Combine(Path.GetTempPath(), $"datasync-enc-{Guid.NewGuid():N}.db");
    }

    /// <summary>The path to the on-disk database file under test.</summary>
    protected string DatabasePath { get; }

    /// <summary>A connection string for <see cref="DatabasePath"/> with pooling disabled.</summary>
    protected string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = DatabasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Pooling = false
    }.ConnectionString;

    /// <summary>Creates the encrypted database and inserts a single <see cref="TodoItem"/>.</summary>
    protected void Seed(string password)
    {
        DbContextOptionsBuilder<PlainTodoContext> builder = new();
        _ = builder.UseEncryptedSqlite(ConnectionString, password);
        using PlainTodoContext context = new(builder.Options);
        _ = context.Database.EnsureCreated();
        _ = context.TodoItems.Add(new TodoItem { Title = "Hello" });
        _ = context.SaveChanges();
    }

    /// <summary>Opens the encrypted database with the supplied key and returns the number of stored items.</summary>
    protected int ReadCount(string password)
    {
        DbContextOptionsBuilder<PlainTodoContext> builder = new();
        _ = builder.UseEncryptedSqlite(ConnectionString, password);
        using PlainTodoContext context = new(builder.Options);
        return context.TodoItems.Count();
    }

    /// <summary>Asserts that the database file on disk does not start with the plaintext SQLite header.</summary>
    protected void AssertFileIsEncrypted()
    {
        byte[] plaintextHeader = "SQLite format 3\0"u8.ToArray();
        byte[] actual = File.ReadAllBytes(DatabasePath);
        _ = actual.Length.Should().BeGreaterThanOrEqualTo(plaintextHeader.Length);
        _ = actual.Take(plaintextHeader.Length).Should().NotEqual(plaintextHeader);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        foreach (string suffix in new[] { string.Empty, "-wal", "-shm", "-journal" })
        {
            try
            {
                string path = DatabasePath + suffix;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup of temporary files; ignore failures.
            }
        }
    }
}
