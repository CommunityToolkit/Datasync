# TodoApp client for MAUI (encrypted offline store)

`TodoApp.MAUI.Encrypted` is a copy of the [MAUI sample](./maui.md) that stores its data in an **encrypted, on-disk** SQLite database instead of an in-memory one. It demonstrates the [encrypted offline store](../../in-depth/client/encryption.md) and, in particular, how to **generate the encryption key on first run and keep it in the platform secure store**.

The original `TodoApp.MAUI` sample is left unchanged; use this project when you want to see encryption wired up end-to-end.

## What is different from `TodoApp.MAUI`

* **Encrypted file store.** The database lives at `Path.Combine(FileSystem.AppDataDirectory, "todoapp.db")` and is configured with `UseEncryptedSqlite` (from the `CommunityToolkit.Datasync.Client.EncryptedSqlite` package) rather than `UseSqlite` over an in-memory connection.
* **Key generated on first run.** `Services/EncryptionKeyProvider.cs` adds a `SecureStorageEncryptionKeyProvider` that, on first launch, generates a cryptographically strong 256-bit key (`RandomNumberGenerator.GetBytes(32)`, base64) and stores it in MAUI [`SecureStorage`](https://learn.microsoft.com/dotnet/maui/platform-integration/storage/secure-storage) (Keychain on iOS/macOS, KeyStore on Android, DPAPI on Windows). Every later launch loads the same key.
* **Key resolved once at start-up.** `App.xaml.cs` resolves the key from a single point before configuring the database, so the first-run generation cannot race.

!!! warning Losing the key means losing the data
    The encrypted database can only be opened with the key it was created with. If the stored key is cleared or changed, the existing offline data becomes permanently unreadable. Treat the key as the root secret protecting the local store.

## Run the application

* [Configure Visual Studio for MAUI development](https://learn.microsoft.com/dotnet/maui/get-started/installation).
* Open `samples/todoapp/Samples.TodoApp.sln` in Visual Studio.
* In the Solution Explorer, right-click the `TodoApp.MAUI.Encrypted` project, then select **Set as Startup Project**.
* Select a target (in the top bar), then press F5 to run the application.

After the first run you can confirm the store is encrypted: the `todoapp.db` file under the app data directory does **not** begin with the plaintext `SQLite format 3` header, and a `todoapp-offline-db-key` entry is present in the platform secure store.

!!! note Referencing the package
    Until `CommunityToolkit.Datasync.Client.EncryptedSqlite` is published to NuGet, this sample references the toolkit projects from source (see the comment in `TodoApp.MAUI.Encrypted.csproj`). Once the package ships, replace those `ProjectReference`s with the corresponding `PackageReference`s.

## Enabling datasync operations

Adding offline synchronization is identical to the [MAUI sample](./maui.md#update-the-application-for-datasync-operations) &mdash; the encryption only changes how the local store is opened, not how push/pull work.
