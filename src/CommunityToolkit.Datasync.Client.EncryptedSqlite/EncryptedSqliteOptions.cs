// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Data.Sqlite;

namespace CommunityToolkit.Datasync.Client.EncryptedSqlite;

/// <summary>
/// Optional cipher configuration for an encrypted SQLite offline store.  These options are only required when you
/// need to deviate from the SQLite3 Multiple Ciphers default cipher - most commonly to open or create a database in
/// a SQLCipher-compatible format.
/// </summary>
/// <remarks>
/// See the <see href="https://utelle.github.io/SQLite3MultipleCiphers/docs/configuration/config_sql_pragmas/">SQLite3
/// Multiple Ciphers PRAGMA reference</see> for the full set of supported cipher schemes and legacy values.
/// </remarks>
public sealed class EncryptedSqliteOptions
{
    /// <summary>
    /// The cipher scheme to use, for example <c>"sqlcipher"</c> to read or write databases that are compatible with
    /// SQLCipher.  When <see langword="null"/> (the default), the SQLite3 Multiple Ciphers default cipher is used.
    /// </summary>
    public string? Cipher { get; set; }

    /// <summary>
    /// The legacy compatibility level for the selected <see cref="Cipher"/>, for example <c>4</c> to match a database
    /// created by SQLCipher version 4.  When <see langword="null"/> (the default), no legacy PRAGMA is issued.
    /// </summary>
    public int? LegacyCompatibility { get; set; }

    /// <summary>
    /// Applies the cipher configuration and encryption key to an already-open <see cref="SqliteConnection"/>.
    /// </summary>
    /// <param name="connection">An open connection to the encrypted database.</param>
    /// <param name="password">The encryption key supplied by the caller.</param>
    public void Apply(SqliteConnection connection, string password)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrEmpty(password);

        // The cipher configuration and key must be applied as the very first statements on the connection: when
        // opening an existing encrypted database, no other statement (not even SELECT quote(...)) can run until the
        // key has been set, otherwise SQLite reports "file is not a database".
        StringBuilder pragmas = new();
        if (Cipher is not null)
        {
            _ = pragmas.Append($"PRAGMA cipher = {SqliteLiteral.Quote(Cipher)};");
        }

        if (LegacyCompatibility is int legacy)
        {
            _ = pragmas.Append($"PRAGMA legacy = {legacy};");
        }

        _ = pragmas.Append($"PRAGMA key = {SqliteLiteral.Quote(password)};");

        using SqliteCommand command = connection.CreateCommand();
#pragma warning disable CA2100 // Values are escaped via SqliteLiteral.Quote; PRAGMA arguments cannot be parameterized.
        command.CommandText = pragmas.ToString();
#pragma warning restore CA2100
        _ = command.ExecuteNonQuery();
    }
}
