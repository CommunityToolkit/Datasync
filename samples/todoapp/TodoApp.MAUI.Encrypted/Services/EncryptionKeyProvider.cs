// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;

namespace TodoApp.MAUI.Services;

/// <summary>
/// Supplies the encryption key for the offline SQLite store.  The key is created once (on first run /
/// fresh install) and then reused for the lifetime of the installation.
/// </summary>
/// <remarks>
///     <para>
///         The same key must be supplied on every launch: the encrypted database can only be opened with the
///         key it was created with, so if the stored key is lost or changed the existing offline data becomes
///         permanently unreadable.  Treat the key as the root secret protecting the local store.
///     </para>
///     <para>
///         Resolve the key from a single, serialized point during start-up (see how the sample calls it once in
///         <c>App</c>).  Calling an implementation concurrently before any key has been stored could let two
///         callers each generate a different key and race to persist it, which would corrupt access to the
///         database.
///     </para>
/// </remarks>
public interface IEncryptionKeyProvider
{
    /// <summary>
    /// Returns the database encryption key, generating and persisting a new one the first time it is called and
    /// returning the previously stored key on every later call.
    /// </summary>
    /// <returns>The encryption key to pass to the encrypted SQLite store (for example via <c>UseEncryptedSqlite</c>).</returns>
    Task<string> GetOrCreateKeyAsync();
}

/// <summary>
/// An <see cref="IEncryptionKeyProvider"/> that persists the key in the platform secure store via MAUI
/// <see cref="SecureStorage"/> (Keychain on iOS/macOS, KeyStore-backed storage on Android, DPAPI on Windows).
/// On first run no key exists, so a cryptographically strong 256-bit key is generated and stored; every later
/// run loads that same key.  The key is never hard-coded or written to ordinary configuration.
/// </summary>
/// <remarks>
/// The key is generated with <see cref="RandomNumberGenerator"/> (a cryptographically secure RNG) and is
/// 256 bits long.  It is Base64-encoded only so that it can be stored and passed as a string; the encoding
/// adds no entropy and is not a security measure.  Because <see cref="SecureStorage"/> is asynchronous, resolve
/// the key before configuring the database (see <c>App</c>) rather than inside a synchronous context callback.
/// </remarks>
public sealed class SecureStorageEncryptionKeyProvider : IEncryptionKeyProvider
{
    /// <summary>
    /// The <see cref="SecureStorage"/> entry name under which the database key is stored.  Changing this value
    /// after a key has been stored is equivalent to losing the key (see <see cref="IEncryptionKeyProvider"/>).
    /// </summary>
    private const string KeyName = "todoapp-offline-db-key";

    /// <inheritdoc />
    public async Task<string> GetOrCreateKeyAsync()
    {
        string? key = await SecureStorage.Default.GetAsync(KeyName);
        if (string.IsNullOrEmpty(key))
        {
            // First run on this install: no key has been stored yet, so generate a fresh 256-bit key from a
            // cryptographically secure RNG and persist it to the platform secure store.  Subsequent launches
            // take the branch above and reuse this exact key (required to open the existing database).
            key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await SecureStorage.Default.SetAsync(KeyName, key);
        }

        return key;
    }
}
