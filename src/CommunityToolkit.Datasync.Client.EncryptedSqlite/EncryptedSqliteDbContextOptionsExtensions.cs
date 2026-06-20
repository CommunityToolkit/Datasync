// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data;
using CommunityToolkit.Datasync.Client.EncryptedSqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring an <b>encrypted</b> SQLite store for an offline Datasync context.
/// </summary>
/// <remarks>
///     <para>
///         These extensions are provided by the <c>CommunityToolkit.Datasync.Client.EncryptedSqlite</c> package.  They
///         configure Entity Framework Core to use SQLite with on-disk encryption provided by the free, open-source
///         (MIT-licensed) <see href="https://github.com/utelle/SQLite3MultipleCiphers">SQLite3 Multiple Ciphers</see>
///         engine - so no paid third-party license (such as the SQLite Encryption Extension) is required.
///     </para>
///     <para>
///         Reference exactly one SQLitePCLRaw bundle in your application.  This package brings the SQLite3 Multiple
///         Ciphers bundle; do not also reference <c>SQLitePCLRaw.bundle_e_sqlite3</c> (the default, unencrypted bundle)
///         or any other bundle, otherwise the duplicate <c>SQLitePCL.Batteries_V2</c> initializers will conflict.
///     </para>
/// </remarks>
public static class EncryptedSqliteDbContextOptionsExtensions
{
    /// <summary>
    /// Configures the context to connect to an encrypted SQLite database using the supplied connection string and
    /// encryption key.  The key is applied via the <c>Password</c> connection-string keyword (emitting
    /// <c>PRAGMA key</c>) using the SQLite3 Multiple Ciphers default cipher.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connectionString">The SQLite connection string, for example <c>"Data Source=app.db"</c>.</param>
    /// <param name="password">The encryption key.  Supply this from secure storage; never hard-code it.</param>
    /// <param name="sqliteOptionsAction">An optional action to configure the underlying SQLite provider options.</param>
    /// <returns>The same builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseEncryptedSqlite(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        string password,
        Action<SqliteDbContextOptionsBuilder>? sqliteOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrEmpty(password);

        SqliteBatteries.EnsureInitialized();
        string keyedConnectionString = new SqliteConnectionStringBuilder(connectionString) { Password = password }.ConnectionString;
        return optionsBuilder.UseSqlite(keyedConnectionString, sqliteOptionsAction);
    }

    /// <summary>
    /// Configures the context to connect to an encrypted SQLite database using a connection that the caller has
    /// already created (and, if required, keyed - for example via
    /// <see cref="EncryptedSqliteFactory.CreateConnection(string, string, EncryptedSqliteOptions?)"/>).  Use this overload
    /// when you need full control over the connection, such as opening a SQLCipher-compatible database or keeping a
    /// single connection alive for the lifetime of the application.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">The SQLite connection to use.  The caller is responsible for disposing it.</param>
    /// <param name="sqliteOptionsAction">An optional action to configure the underlying SQLite provider options.</param>
    /// <returns>The same builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseEncryptedSqlite(
        this DbContextOptionsBuilder optionsBuilder,
        SqliteConnection connection,
        Action<SqliteDbContextOptionsBuilder>? sqliteOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(connection);

        SqliteBatteries.EnsureInitialized();
        return optionsBuilder.UseSqlite(connection, sqliteOptionsAction);
    }

    /// <summary>
    /// Changes the encryption key of an existing encrypted SQLite database by issuing <c>PRAGMA rekey</c>.
    /// </summary>
    /// <param name="connection">A connection to the encrypted database.  It will be opened if it is not already open.</param>
    /// <param name="newPassword">The new encryption key.  Supply this from secure storage; never hard-code it.</param>
    public static void RekeyEncryptedSqlite(this SqliteConnection connection, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrEmpty(newPassword);

        SqliteBatteries.EnsureInitialized();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using SqliteCommand command = connection.CreateCommand();

        // PRAGMA rekey is not supported in WAL journal mode (the SQLite3 Multiple Ciphers build opens databases in
        // WAL by default), so switch to a rollback journal before re-keying.
        command.CommandText = "PRAGMA journal_mode = DELETE;";
        _ = command.ExecuteNonQuery();

#pragma warning disable CA2100 // The key is escaped via SqliteLiteral.Quote; PRAGMA arguments cannot be parameterized.
        command.CommandText = $"PRAGMA rekey = {SqliteLiteral.Quote(newPassword)};";
#pragma warning restore CA2100
        _ = command.ExecuteNonQuery();
    }
}
