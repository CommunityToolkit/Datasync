# Blazor WASM Support 

You can use Blazor WASM with the Datasync Community Toolkit; however, there are some signficant problems:

1. In the general case, you cannot use SQLite for offline storage.
2. You will have to suppress the `WASM0001` warning for sqlite.

## Do not use offline mode

It is possible to use offline mode by storing the SQLite database in local storage within the browser.  When you start up your application, you restore the SQLite in-memory database into memory; on a regular basis, you write the SQLite database to local storage.  However, this system severely affects performance within the browser and large synchronizations will cause the local storage to overflow.  As a result of these problems, it is not recommended, nor is it supported.

!!! warning "Known limitations of offline mode on browser-wasm"
    If you do experiment with offline mode in the browser, be aware of the following single-threaded browser-wasm limitations.  These are not currently fixed because offline mode in Blazor WASM is not supported.

    * **Use the async APIs.**  The synchronous `SaveChanges()` calls `OperationsQueueManager.UpdateOperationsQueue()`, which blocks on a thread-pool task and cannot work on single-threaded WASM.  Use `SaveChangesAsync()` and the `PullAsync()` / `PushAsync()` methods instead.
    * **Specify a query ID for filtered pulls.**  When a pull query has an OData filter and you do not supply a query ID, the toolkit generates one using `MD5`, which is marked as unsupported on `browser`.  Pass an explicit query ID to avoid this.

    Blocking waits on `SemaphoreSlim` (which throw on .NET 11 browser-wasm) are no longer used when building the offline options, so the first `PullAsync()` call no longer fails for that reason.

## Suppress the `WASM0001` warning

When compiling the WASM client, you will see warning `WASM0001`.  This is a harmless warning that indicates SQLite is not available.  However, you may be running using "Warnings as Errors".  The most appropriate solution is to suppress the `WASM0001` warning in your client `.csproj` file as follows:

```xml
<PropertyGroup>
    <NoWarn>$(NoWarn);WASM0001</NoWarn>
</PropertyGroup>
```

Alternatively, you may also use the following to keep `WASM0001` as a warning even when using "Warnings as Errors":

```xml
<PropertyGroup>
  <WarningsNotAsErrors>$(WarningsNotAsErrors);WASM0001</WarningsNotAsErrors>
</PropertyGroup>
```

This will suppress the harmless SQLite warning that appears when building Blazor WASM applications that reference libraries containing SQLite dependencies (even when not using SQLite directly).

## Sample

We have a Blazor WASM sample in our sample set: [samples/todoapp-blazor-wasm](https://github.com/CommunityToolkit/Datasync/tree/main/samples/todoapp-blazor-wasm).  This sample comprises a server (which uses an in-memory EF Core database) combined with a Blazor Web Assembly client.
