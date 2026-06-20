// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.EncryptedSqlite;

/// <summary>
/// Ensures the SQLite3 Multiple Ciphers (SQLite3MC) native provider is registered with SQLitePCLRaw.
/// </summary>
/// <remarks>
/// The encryption-enabled offline store references the bundle-less <c>Microsoft.EntityFrameworkCore.Sqlite.Core</c>
/// provider together with the <c>SQLite3MC.PCLRaw.bundle</c> native package. Unlike the full
/// <c>Microsoft.EntityFrameworkCore.Sqlite</c> package, the <c>.Core</c> variant does not automatically register a
/// SQLitePCLRaw provider, so <see cref="SQLitePCL.Batteries_V2.Init"/> must be called once - before any
/// <see cref="Microsoft.Data.Sqlite.SqliteConnection"/> is opened - to activate the SQLite3MC provider.
/// </remarks>
internal static class SqliteBatteries
{
    private static readonly object gate = new();
    private static bool initialized;

    /// <summary>
    /// Registers the SQLite3MC provider exactly once for the lifetime of the process.
    /// </summary>
    internal static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        lock (gate)
        {
            if (!initialized)
            {
                SQLitePCL.Batteries_V2.Init();
                initialized = true;
            }
        }
    }
}
