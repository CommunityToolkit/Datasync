# Encrypted offline store

The offline store is a standard Entity Framework Core SQLite database, so by default it is **not** encrypted. If your application stores sensitive data offline, you can encrypt the database file on disk.

## Background: SQLitePCLRaw 3 and encryption

Historically, applications enabled SQLite encryption by swapping the default `SQLitePCLRaw.bundle_e_sqlite3` bundle for `SQLitePCLRaw.bundle_e_sqlcipher`. As of **SQLitePCLRaw 3.0** (pulled in by EF Core 10), the free encryption bundles (`bundle_e_sqlcipher` and `bundle_e_sqlite3mc`) are no longer distributed, and the maintainer's recommended replacement &mdash; the SQLite Encryption Extension (SEE) &mdash; requires a **paid license**.

The `CommunityToolkit.Datasync.Client.EncryptedSqlite` package provides encryption **without a paid third-party license** by using [SQLite3 Multiple Ciphers](https://github.com/utelle/SQLite3MultipleCiphers) (SQLite3MC) &mdash; an open-source, MIT-licensed encryption engine that is compatible with SQLCipher database files.

!!! warning Reference exactly one SQLitePCLRaw bundle
    A project may reference only **one** SQLitePCLRaw bundle. The base `CommunityToolkit.Datasync.Client` package uses the bundle-less `Microsoft.EntityFrameworkCore.Sqlite.Core` provider so that you can choose the native library:

    * For a **plaintext** offline store, add `SQLitePCLRaw.bundle_e_sqlite3`.
    * For an **encrypted** offline store, add `CommunityToolkit.Datasync.Client.EncryptedSqlite` (which brings the SQLite3MC bundle).

    Do not reference both. Two bundles produce a duplicate `SQLitePCL.Batteries_V2` and will not compile.

## Set up

1. Install the `CommunityToolkit.Datasync.Client.EncryptedSqlite` package from NuGet. Do not add any other SQLitePCLRaw bundle to the application.

2. Configure your `OfflineDbContext` to use the encrypted store via `UseEncryptedSqlite`, supplying the encryption key from secure storage (for example the platform keychain/keystore):

        public class AppDbContext : OfflineDbContext
        {
            private readonly string encryptionKey;

            public AppDbContext(string encryptionKey)
            {
                this.encryptionKey = encryptionKey;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (!optionsBuilder.IsConfigured)
                {
                    optionsBuilder.UseEncryptedSqlite("Data Source=app.db", this.encryptionKey);
                }

                base.OnConfiguring(optionsBuilder);
            }

            protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseEndpoint(new Uri("https://YOURSITEHERE.azurewebsites.net/"));
            }

            public DbSet<TodoItem> TodoItems => Set<TodoItem>();
        }

    The `UseEncryptedSqlite` extension lives in the `Microsoft.EntityFrameworkCore` namespace (like the built-in `UseSqlite`), so no additional `using` directive is required for the common case.

!!! note Never hard-code the key
    The encryption key should come from a secure source such as the device keychain/keystore, a user-derived passphrase, or a secret store. Do not hard-code it in source or configuration.

## Generate and store the key on first run

The toolkit does not generate or store the key for you &mdash; that is the application's responsibility. A common pattern is to **generate a random key the first time the application runs and store it in the platform secure store**, then reuse the same key on every later launch. The database can only be opened with that key, so keep it safe and consider backing it up if losing it would mean losing the data.

On .NET MAUI, use [`SecureStorage`](https://learn.microsoft.com/dotnet/maui/platform-integration/storage/secure-storage) (backed by the iOS/macOS Keychain, the Android KeyStore, and Windows DPAPI):

        using System.Security.Cryptography;
        using Microsoft.Maui.Storage;

        public static class EncryptionKeyProvider
        {
            private const string KeyName = "offline-db-key";

            public static async Task<string> GetOrCreateKeyAsync()
            {
                string? key = await SecureStorage.Default.GetAsync(KeyName);
                if (string.IsNullOrEmpty(key))
                {
                    // First run: generate a 256-bit key and persist it to the secure store.
                    key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                    await SecureStorage.Default.SetAsync(KeyName, key);
                }

                return key;
            }
        }

Resolve the key before configuring the context (for example during start-up) and pass it to `UseEncryptedSqlite`. The [TodoApp.MAUI.Encrypted sample](../../samples/todoapp/maui-encrypted.md) wires this up end-to-end.

### On other platforms

`SecureStorage` is MAUI-only. On other clients, generate the key the same way (`RandomNumberGenerator.GetBytes(32)`, base64-encoded, created once and reused) but persist it in that platform's secure store:

* **Windows / WinUI / WPF** &mdash; Windows Credential Locker (`PasswordVault`) or DPAPI (`ProtectedData`).
* **macOS / iOS** &mdash; the Keychain.
* **Android** &mdash; the Android KeyStore (for example via EncryptedSharedPreferences).
* **Linux** &mdash; the Secret Service API / libsecret (GNOME Keyring, KWallet).
* **Avalonia / Uno Platform** &mdash; use the platform options above, or a community secure-storage plugin for your framework.

In every case the rule is the same: generate once on first run, store it securely, and never hard-code it.

## Changing the key

Use `RekeyEncryptedSqlite` on a connection opened with the current key to re-encrypt the database with a new key:

        using CommunityToolkit.Datasync.Client.EncryptedSqlite;

        using SqliteConnection connection = EncryptedSqliteFactory.CreateConnection("Data Source=app.db", currentKey);
        connection.RekeyEncryptedSqlite(newKey);

## Opening an existing SQLCipher database

If you are migrating from a database created with SQLCipher, select the `sqlcipher` cipher (and the matching legacy compatibility level) when opening the connection, then pass that connection to `UseEncryptedSqlite`:

        using CommunityToolkit.Datasync.Client.EncryptedSqlite;

        EncryptedSqliteOptions options = new() { Cipher = "sqlcipher", LegacyCompatibility = 4 };
        SqliteConnection connection = EncryptedSqliteFactory.CreateConnection("Data Source=legacy.db", key, options);

        // The caller owns the connection and is responsible for disposing it.
        optionsBuilder.UseEncryptedSqlite(connection);

The `CreateConnection` helper opens and keys the connection for you; because the context does not own the connection, dispose it yourself when you are finished.

## Support and further information

For more information about the underlying encryption engine and the available cipher schemes, review the [SQLite3 Multiple Ciphers documentation](https://utelle.github.io/SQLite3MultipleCiphers/).
