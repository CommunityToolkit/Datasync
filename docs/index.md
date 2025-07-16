# Welcome to the Datasync Community Toolkit

The [Datasync Community Toolkit][nuget] is a collection of libraries that implement a client-server system used for synchronizing data from the database table. The Datasync Community Toolkit is a member of the [Community Toolkit organization][org-github] and published under [the dotnet Foundation][dotnetfdn].

Currently, the library supports:

* ASP.NET Core 8.x or later.
* .NET clients using .NET 8.x or later.

> Ensure you match the version of the Datasync Community Toolkit to the version of .NET you are using.

Client platforms that have been tested include:

* [Avalonia][avalonia]
* [Blazor WASM][blazor-wasm]
* [.NET MAUI][maui]
* [Uno Platform][uno]
* [Windows Presentation Framework (WPF)][wpf]
* [Windows UI Library (WinUI) 3][winui3]

Database support include Azure SQL, Cosmos, LiteDb, MongoDb, MySQL, PostgreSQL, SQLite, and an in-memory store.  Additional database support is easy to add through Entity Framework Core or your own repository implementation.

There is no platform-specific code within the client library.  It should work with any .NET based client technology.  However, we only test a limited number of platforms.

## Getting started

You can easily get started by creating your own server.  The server is based on ASP.NET Core and can run anywhere you run such applications.  Use our template:

```bash
dotnet new -i CommunityToolkit.Datasync.Server.Template.CSharp
dotnet new datasync-server -n My.Datasync.Server
```

## Go deeper

Find out more about developing datasync applications:

* [The client](./in-depth/client/index.md)
* [The server](./in-depth/server/index.md)
* [Samples](./samples/todoapp/server.md)

<!-- Links -->
[nuget]: https://www.nuget.org/packages?q=CommunityToolkit.Datasync
[org-github]: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/
[dotnetfdn]: https://dotnetfoundation.org/
[avalonia]: https://www.avaloniaui.net/
[blazor-wasm]: https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot
[maui]: https://dotnet.microsoft.com/apps/maui
[uno]: https://platform.uno/
[wpf]: https://learn.microsoft.com/dotnet/desktop/wpf/overview/
[winui3]: https://learn.microsoft.com/windows/apps/winui/winui3/
