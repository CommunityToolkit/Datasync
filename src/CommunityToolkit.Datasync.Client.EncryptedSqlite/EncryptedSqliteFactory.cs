// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Data.Sqlite;

namespace CommunityToolkit.Datasync.Client.EncryptedSqlite;

/// <summary>
/// Helpers for working with encrypted SQLite databases backed by the free, open-source
/// <see href="https://github.com/utelle/SQLite3MultipleCiphers">SQLite3 Multiple Ciphers</see> engine.
/// </summary>
public static class EncryptedSqliteFactory
{
    /// <summary>
    /// Registers the SQLite3 Multiple Ciphers provider with SQLitePCLRaw.  This is called automatically by the
    /// <c>UseEncryptedSqlite</c> extension methods and <see cref="CreateConnection"/>; call it yourself only if you
    /// open a <see cref="SqliteConnection"/> directly before using any of those helpers.
    /// </summary>
    public static void Initialize()
        => SqliteBatteries.EnsureInitialized();

    /// <summary>
    /// Creates and opens an encrypted <see cref="SqliteConnection"/>.  Use this when you need full control of the
    /// connection - for example to open a SQLCipher-compatible database via <paramref name="options"/>, or to keep a
    /// single connection alive for the lifetime of the application.  The caller owns the returned connection and is
    /// responsible for disposing it.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string, for example <c>"Data Source=app.db"</c>.</param>
    /// <param name="password">The encryption key.  Supply this from secure storage; never hard-code it.</param>
    /// <param name="options">Optional cipher configuration (for example to use a SQLCipher-compatible format).</param>
    /// <returns>An open, keyed <see cref="SqliteConnection"/>.</returns>
    public static SqliteConnection CreateConnection(string connectionString, string password, EncryptedSqliteOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrEmpty(password);

        SqliteBatteries.EnsureInitialized();

        if (options is null)
        {
            // The Password keyword causes Microsoft.Data.Sqlite to emit PRAGMA key on open using the default cipher.
            SqliteConnectionStringBuilder builder = new(connectionString) { Password = password };
            SqliteConnection connection = new(builder.ConnectionString);
            connection.Open();
            return connection;
        }
        else
        {
            // Apply the cipher configuration and key explicitly so that a non-default (for example SQLCipher) cipher
            // is selected before the key is set.
            SqliteConnection connection = new(connectionString);
            connection.Open();
            options.Apply(connection, password);
            return connection;
        }
    }
}
